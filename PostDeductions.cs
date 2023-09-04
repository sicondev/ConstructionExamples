using Sicon.Sage200.Construction.Objects;
using Sicon.Sage200.Construction.Objects.SageObjects;
using Sicon.Sage200.Construction.Objects.Retentions.Factories;
using System;
using Sicon.Sage200.Construction.Objects.CIS.Factories;
using Sicon.Sage200.Construction.Objects.CIS.Repositories;

namespace ConstructionExamples
{
    public class PostDeductions
    {
        ////Post deductions for Purchase transaction in Sicon Construction (v21.1+)

        /// <summary>
        /// Get CIS Subcontractor details for a supplier
        /// </summary>
        /// <param name="PLSupplierAccountID"></param>
         /// <returns></returns>
        public SiconCISSupplier GetCISSupplier(long PLSupplierAccountID)
        {
             SiconCISSupplier oSiconCISSupplier = null;
             try
             {
                 //Get CIS Supplier
                oSiconCISSupplier = SiconCISSupplierFactory.Factory.FetchSiconCISSupplier(PLSupplierAccountID);
                return oSiconCISSupplier;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// check if supplier is a CIS Subcontractor
        /// </summary>
        /// <param name="PLSupplierAccountID"></param>
        /// <returns></returns>
        public bool CheckIfCISSubcontractor(long PLSupplierAccountID)
        {
            SiconCISSupplier oSiconCISSupplier = null;
            try
            {
                //Get CIS Supplier
                oSiconCISSupplier = SiconCISSupplierFactory.Factory.FetchSiconCISSupplier(PLSupplierAccountID);
                //Return bool checking if its a subbie. (Sicon.Sage200.Construction.Objects.CIS.Repositories)
                return CISCommon.IsCISSubcontractor(oSiconCISSupplier);
            }
            catch (Exception)
            {
                 throw;
            }
        }

        /// <summary>
        /// Get Labour, material and other value split for a PL transaction
        /// </summary>
        /// <param name="URN"></param>
        /// <param name="companyID"></param>
        public void CalculateTransactionCISValues(long URN, int? companyID = null)
        {
            SiconCISSupplier oSiconCISSupplier = null;
            Objects.Configs.Global oGlobal = null;
            Sage.Accounting.PurchaseLedger.PostedPurchaseAccountEntries oPLEntries = null;
            Sage.Accounting.PurchaseLedger.PostedPurchaseAccountEntry oPLEntry = null;
            try
            {
                oGlobal = new Objects.Configs.Global();

                //Find Transaction With URN
                oPLEntries = Sage.Accounting.PurchaseLedger.PostedPurchaseAccountEntriesFactory.Factory.CreateNew();
                oPLEntries.Query.Filters.Add(new Sage.ObjectStore.Filter("UniqueReferenceNumber", URN));
                oPLEntries.Find();
                if (oPLEntries.IsEmpty)
                {
                    throw new Exception("Purchase entry with URN '" + URN + "' could not be found");
                }
                else
                {
                    oPLEntry = oPLEntries.First;
                }

                //Get Settings
                oSiconCISSupplier = SiconCISSupplierFactory.Factory.FetchSiconCISSupplier(oPLEntry.PLSupplierAccountID);

                //Calculate Splits
                decimal thisLabourAmount = 0;
                decimal thisMaterialsAmount = 0;
                decimal thisOtherAmount = 0;
                if (oGlobal.IsManualDeductionEnabled())
                {
                    thisLabourAmount = (oPLEntry.DocumentNetValue / 100) * oSiconCISSupplier.DefaultLabourPercentage;
                    thisMaterialsAmount = (oPLEntry.DocumentNetValue - thisLabourAmount);
                    thisOtherAmount = 0;
                }
                else
                {
                    //for each nominal entry, check for valid entries to credit
                    foreach (Sage.Accounting.NominalLedger.NominalAccountEntryView oNLView in oPLEntry.TransactionDrillDown.NominalEntries)
                    {
                        //Check if Debtors control or Tax
                        if (!oGlobal.IsCreditorsOrTaxControlNominalAccount(oNLView))
                        {
                            //Get nominal spec 
                            Sage.Accounting.Common.NominalSpecification oNLSpec = Sage.Accounting.Common.NominalSpecificationFactory.Factory.CreateNew(oNLView.AccountNumber, oNLView.AccountCostCentre, oNLView.AccountDepartment);
                            //Get Nominal Code
                            Sage.Accounting.NominalLedger.NominalCode oNLCode = Sage.Accounting.NominalLedger.NominalCodeFactory.Factory.Fetch(oNLSpec);


                            if (CISCommon.IsLabourNominalCode(oNLCode) == true)
                            {
                                thisLabourAmount += oNLView.GoodsValueInBaseCurrency;
                            }
                            else if (CISCommon.IsMaterialsNominalCode(oNLCode))
                            {
                                thisMaterialsAmount += oNLView.GoodsValueInBaseCurrency;
                            }
                            else
                            {
                                thisOtherAmount += oNLView.GoodsValueInBaseCurrency;
                            }
                        }
                    }
                }

                //Use Data collected in PostDeductionsMethod
            }
            catch (Exception)
            {

                throw;
            }
        }

        
        /// <summary>
        /// Method example for posting CIS/CITB/Retention deductions
        /// </summary>
        /// <param name="PLSupplierAccountID">Supplier Account ID</param>
        /// <param name="Net">Net Value of an Invoice or credit</param>
        /// <param name="nominalAccountEntryViews">The nominal items from the invoice/credit</param>
        public void PostDeductionsMethod(long PLSupplierAccountID, decimal Net, Sage.Accounting.NominalLedger.NominalAccountEntryViews nominalAccountEntryViews)
        {
            SiconRetentionSupCusSetting oSetting = null;
            Sicon.Sage200.Construction.Objects.Configs.Global oGlobal = null;
            SiconCISSupplier oSiconCISSupplier = null;
            Sicon.Sage200.Construction.Objects.Infrastructure.CIS oCISCommon = null;
            Sicon.API.Sage200.Objects.NominalLedger.Common oNLCommon = null;
            try
            {
                oGlobal = new Sicon.Sage200.Construction.Objects.Configs.Global();
                oCISCommon = new Sicon.Sage200.Construction.Objects.Infrastructure.CIS();
                oNLCommon = new Sicon.API.Sage200.Objects.NominalLedger.Common();

                long URN = 10101;//URN of posted PL Invoice or credit note

                //Retention Fields (If Retentions are enabled)
                decimal RetentionPercentage1 = 0;
                decimal RetentionPercentage2 = 0;
                decimal RetentionPercentage3 = 0;
                decimal RetentionPercentage4 = 0;
                DateTime RetentionDueDate1 = DateTime.MinValue;
                DateTime RetentionDueDate2 = DateTime.MinValue;
                DateTime RetentionDueDate3 = DateTime.MinValue;
                DateTime RetentionDueDate4 = DateTime.MinValue;


                //Get PL retention settings and apply defaults
                oSetting = RetentionSupCusSettingFactory.Factory.FetchBySupplierID(PLSupplierAccountID);
                if (oSetting != null)
                {
                    RetentionPercentage1 = oSetting.RetentionPercentage;
                    RetentionPercentage2 = oSetting.RetentionPercentage2;
                    RetentionPercentage3 = oSetting.RetentionPercentage3;
                    RetentionPercentage4 = oSetting.RetentionPercentage4;

                    RetentionDueDate1 = oSetting.RetentionDuration1 != 0 ? Sicon.API.Sage200.Objects.StaticClass.GetCurrentDateTime().AddDays(oSetting.RetentionDuration1) : DateTime.MinValue;
                    RetentionDueDate2 = oSetting.RetentionDuration2 != 0 ? Sicon.API.Sage200.Objects.StaticClass.GetCurrentDateTime().AddDays(oSetting.RetentionDuration2) : DateTime.MinValue;
                    RetentionDueDate3 = oSetting.RetentionDuration3 != 0 ? Sicon.API.Sage200.Objects.StaticClass.GetCurrentDateTime().AddDays(oSetting.RetentionDuration3) : DateTime.MinValue;
                    RetentionDueDate4 = oSetting.RetentionDuration4 != 0 ? Sicon.API.Sage200.Objects.StaticClass.GetCurrentDateTime().AddDays(oSetting.RetentionDuration4) : DateTime.MinValue;
                }

                //CIS/CITB Fields (If CIS is enabled)

                oSiconCISSupplier = SiconCISSupplierFactory.Factory.FetchSiconCISSupplier(PLSupplierAccountID);
                decimal LabourPC = 0;
                decimal MaterialPC = 0;
                decimal OtherPC = 0;
                decimal LabourValue = 0;
                decimal MaterialsValue = 0;
                decimal OtherValue = 0;

                //OPTIONAL: If you want to do CIS based on Percentage or by nominal with default configurations (Or specify your own values)
                if (oGlobal.IsManualDeductionEnabled())
                {
                    //get %'s
                    LabourPC = oSiconCISSupplier.DefaultLabourPercentage;
                    MaterialPC = (100 - oSiconCISSupplier.DefaultLabourPercentage);
                    OtherPC = 0;

                    //Calculate Values
                    LabourValue = (Net / 100) * LabourPC;
                    MaterialsValue = (Net / 100) * MaterialPC;
                    OtherValue = (Net / 100) * OtherPC;
                }
                else
                {
                    //Loop through nominal lines on transaction
                    foreach (Sage.Accounting.NominalLedger.NominalAccountEntryView nominalAccountEntryView in nominalAccountEntryViews)
                    {
                        if (oGlobal.IsCreditorsOrTaxControlNominalAccount(nominalAccountEntryView) == false)
                        {
                            if (oCISCommon.IsLabourNominalCode(oNLCommon.GetNominalCode(nominalAccountEntryView.AccountNumber, nominalAccountEntryView.AccountCostCentre, nominalAccountEntryView.AccountDepartment)) == true)
                            {
                                LabourValue = LabourValue + nominalAccountEntryView.GoodsValueInDocumentCurrency;
                            }
                            else
                            {
                                if (oCISCommon.IsMaterialsNominalCode(oNLCommon.GetNominalCode(nominalAccountEntryView.AccountNumber, nominalAccountEntryView.AccountCostCentre, nominalAccountEntryView.AccountDepartment)) == true)
                                {
                                    MaterialsValue = MaterialsValue + nominalAccountEntryView.GoodsValueInDocumentCurrency;
                                }
                                else
                                {
                                    OtherValue = OtherValue + nominalAccountEntryView.GoodsValueInDocumentCurrency;
                                }
                            }
                        }
                    }

                    //get %'s
                    LabourPC = (LabourValue / Net) * 100;
                    MaterialPC = (MaterialsValue / Net) * 100;
                    OtherPC = (OtherValue / Net) * 100;
                }


                //Post Deductions
                Sicon.Sage200.Construction.Objects.Infrastructure.Common.PostDeductionsForTransaction(URN, RetentionPercentage1, RetentionDueDate1, RetentionPercentage2, RetentionDueDate2, RetentionPercentage3, RetentionDueDate3, RetentionPercentage4, RetentionDueDate4, LabourValue, MaterialsValue, OtherValue);

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
