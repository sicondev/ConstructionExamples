using System;

namespace ConstructionExamples
{
    public class AutoAllocateCIS
    {
        ////Post deductions for Purchase transaction in Sicon Construction (v21.1+)

        /// <summary>
        /// Example of auto allocating a transaction to its deduction posting. If Authorisation is enabled then This wont normally happen untill the dicument has been authorised as Sage wont allow allocations.
        /// </summary>
        /// <param name="IsCredit">Determins if original transaction is a Credit Note</param>
        /// <param name="SupplierReference">Reference of the supplier</param>
        /// <param name="AuthorisedURN">URN of Autorised transaction (original not the deduction)</param>
        public void AutoAllocateCISPosting(bool IsCredit, string SupplierReference, long AuthorisedURN)
        {
            Sicon.Sage200.Construction.Objects.Infrastructure.CIS oCISCommon = null;
            Sicon.Sage200.Construction.Objects.Configs.Global oGlobal = null;
            try
            {
                oCISCommon = new Sicon.Sage200.Construction.Objects.Infrastructure.CIS();
                oGlobal = new Sicon.Sage200.Construction.Objects.Configs.Global();

                // AuthorisedURN = The URN of the original transaction
                if (IsCredit)//If Original document is a credit note
                {
                    using (Sicon.Sage200.Construction.Objects.SageObjects.SiconCISAudit thisSiconCISAudit = oGlobal.GetSiconCISAuditCN(AuthorisedURN))
                    {
                        if (thisSiconCISAudit != null)
                        {
                            try
                            {
                                oCISCommon.AutoAllocateCISDeduction(SupplierReference, thisSiconCISAudit.INV_URN, AuthorisedURN, Sicon.Sage200.Construction.Objects.Infrastructure.CIS.CISDocumentType.Invoice);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("CIS Invoice with URN " + thisSiconCISAudit.INV_URN + " is not available for allocation." +
                                    Environment.NewLine + Environment.NewLine + "Error: " + ex.Message);
                            }
                        }
                    }
                }
                else
                {
                    using (Sicon.Sage200.Construction.Objects.SageObjects.SiconCISAudit thisSiconCISAudit = oGlobal.GetSiconCISAudit(AuthorisedURN))
                    {
                        if (thisSiconCISAudit != null)
                        {
                            try
                            {
                                oCISCommon.AutoAllocateCISDeduction(SupplierReference, AuthorisedURN, thisSiconCISAudit.CRN_URN, Sicon.Sage200.Construction.Objects.Infrastructure.CIS.CISDocumentType.CreditNote);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("CIS credit with URN " + thisSiconCISAudit.CRN_URN + " is not available for allocation." +
                                    Environment.NewLine + Environment.NewLine + "Error: " + ex.Message);
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
