using Sicon.Sage200.Construction.Objects;
using Sicon.Sage200.Construction.Objects.SageObjects;
using Sicon.Sage200.Construction.Objects.Retentions.Factories;
using System;
using Sicon.Sage200.Construction.Objects.CIS.Factories;

namespace ConstructionExamples
{
    public class PostDeductions
    {
        ////Post deductions for Purchase transaction in Sicon Construction (v21.1+)

        /// <summary>
        /// Method example for posting CIS/CITB/Retention deductions
        /// </summary>
        /// <param name="PLSupplierAccountID">Supplier Account ID</param>
        /// <param name="Net">Net Value of an Invoice or credit</param>
        /// <param name="nominalAccountEntryViews">The nominal items from the invoice/credit</param>
        public PostDeductions(long PLSupplierAccountID, decimal Net, Sage.Accounting.NominalLedger.NominalAccountEntryViews nominalAccountEntryViews)
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
