using Sicon.Sage200.Construction.Objects.SageObjects;
using Sicon.Sage200.Construction.Objects.Retentions.Factories;
using System;

namespace ConstructionExamples
{
    public class Retentions
    {
        ////Post deductions for Purchase transaction in Sicon Construction (v21.1+)

        /// <summary>
        /// Example of getting Retention Settings
        /// </summary>
        /// <param name="PLSupplierAccountID">Supplier ID</param>
        /// <returns></returns>
        public SiconRetentionSupCusSetting GetRetentionsSettings(long PLSupplierAccountID)
        {
            return RetentionSupCusSettingFactory.Factory.FetchBySupplierID(PLSupplierAccountID);
        }
    }
}
