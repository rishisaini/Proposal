using AssetCare.Model.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AssetCare.Model.Common
{
    public class HelperMethods
    {
        public static bool DisableField(JobUserCapabilities userCapabilities, string section, string field)
        {
            string fieldData = "";
            switch (section)
            {
                case "mainSection":
                    fieldData = userCapabilities.MainSection;
                    break;
                case "detailSection":
                    fieldData = userCapabilities.DetailSection;
                    break;
                case "estimateSection":
                    fieldData = userCapabilities.EstimateSection;
                    break;
                case "costingSection":
                    fieldData = userCapabilities.CostingSection;
                    break;
                case "recordSection":
                    fieldData = userCapabilities.RecordSection;
                    break;
                case "claimSection":
                    fieldData = userCapabilities.ClaimSection;
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    bool available = fieldsArray.Any(x => x == field);
                    return !available;
                }
            }

            return true;
        }

        public static bool DisableBIReportField(BIReportUserCapabilities userCapabilities, string section, string field)
        {
            string fieldData = "";
            switch (section)
            {
                case "FMWorksReports":
                    fieldData = userCapabilities.FMWorksReports;
                    break;
                case "PMWorksReports":
                    fieldData = userCapabilities.PMWorksReports;
                    break;
                case "IRCMReports":
                    fieldData = userCapabilities.IRCMReports;
                    break;
                case "AssetRegisterReports":
                    fieldData = userCapabilities.AssetRegisterReports;
                    break;
                case "TreeSafetyReports":
                    fieldData = userCapabilities.TreeSafetyReports;
                    break;
                case "ComplainceReports":
                    fieldData = userCapabilities.ComplainceReports;
                    break;
                case "GeneralReports":
                    fieldData = userCapabilities.GeneralReports;
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    bool available = fieldsArray.Any(x => x == field);
                    return !available;
                }
            }

            return true;
        }

        public static string GetFinancialYear(DateTime dateTime)
        {
            if (dateTime.Month >= 7)
            {
                return dateTime.Year + "/" + dateTime.Year + 1;
            }
            else
            {
                return dateTime.Year - 1 + "/" + dateTime.Year;

            }

        }

        public void updateChangedFields(string fieldData, Object source, Object destination)
        {
            string[] fields;

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    fields = fieldsArray;
                    foreach (string field in fields)
                    {
                        SetPropertyValue(source, destination, field, GetPropertyValue(source.GetType(), source, field));
                    }
                }
            }

        }

        public string RemoveUserCapability(string sectionData, string key)
        {
            if (!string.IsNullOrEmpty(sectionData))
            {
                if (sectionData.Contains(key))
                {
                    sectionData = sectionData.Replace(key, string.Empty);
                }
            }

            return sectionData;
        }

        public static bool DisableField(DefectCallUserCapabilities userCapabilities, string section, string field)
        {
            string fieldData = "";
            switch (section)
            {
                case "mainSection":
                    fieldData = userCapabilities.MainSection;
                    break;
                case "responseTimeSection":
                    fieldData = userCapabilities.ResponseTimeSection;
                    break;
                case "PMSection":
                    fieldData = userCapabilities.PMSection;
                    break;
                case "documentSection":
                    fieldData = userCapabilities.DocumentSection;
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    bool available = fieldsArray.Any(x => x == field);
                    return !available;
                }
            }

            return true;
        }

        public static bool DisableProposalField(ProposalUserCapabilities userCapabilities, string section, string field)
        {
            string fieldData = "";
            switch (section)
            {
                case "schoolsection":
                    fieldData = userCapabilities.SchoolSection;
                    break;
                case "detailsection":
                    fieldData = userCapabilities.DetailSection;
                    break;
                case "schoolendorsement":
                    fieldData = userCapabilities.SchoolEndorsement;
                    break;
                case "entitlementsection":
                    fieldData = userCapabilities.EntitlementSection;
                    break;
                case "claimSection":
                    fieldData = userCapabilities.ClaimSection;
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    bool available = fieldsArray.Any(x => x == field);
                    return !available;
                }
            }

            return true;
        }


        public static bool DisableField(CallOutUserCapabilities userCapabilities, string section, string field)
        {
            string fieldData = "";
            switch (section)
            {
                case "mainSection":
                    fieldData = userCapabilities.MainSection;
                    break;
                case "responseTimeSection":
                    fieldData = userCapabilities.ResponseTimeSection;
                    break;
                case "callCenterSection":
                    fieldData = userCapabilities.CallCenterSection;
                    break;
                case "ircmSection":
                    fieldData = userCapabilities.IRCMSection;
                    break;
                case "documentSection":
                    fieldData = userCapabilities.DocumentSection;
                    break;
                case "capitalDefectSection":
                    fieldData = userCapabilities.CapitalDefectSection;
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    bool available = fieldsArray.Any(x => x == field);
                    return !available;
                }
            }

            return true;
        }

        public static bool DisableField(ProWOUserCapabilities userCapabilities, string section, string field)
        {
            string fieldData = "";
            switch (section)
            {
                case "mainSection":
                    fieldData = userCapabilities.MainSection;
                    break;
                case "detailSection":
                    fieldData = userCapabilities.DetailSection;
                    break;
                case "costingSection":
                    fieldData = userCapabilities.CostingSection;
                    break;
                case "recordSection":
                    fieldData = userCapabilities.RecordSection;
                    break;
                case "claimSection":
                    fieldData = userCapabilities.ClaimSection;
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    bool available = fieldsArray.Any(x => x == field);
                    return !available;
                }
            }

            return true;
        }


        public static bool DisableField(FMCServiceFeeUserCapabilities userCapabilities, string section, string field)
        {
            string fieldData = "";
            switch (section)
            {
                case "claimSection":
                    fieldData = userCapabilities.ClaimSection;
                    break;
                case "approvalSection":
                    fieldData = userCapabilities.ApprovalSection;
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(fieldData))
            {
                string[] fieldsArray = fieldData.Split(',');
                if (fieldsArray != null)
                {
                    bool available = fieldsArray.Any(x => x == field);
                    return !available;
                }
            }

            return true;
        }

        private object GetPropertyValue(Type type, Object data, string field)
        {

            PropertyInfo prop = type.GetProperty(field);
            if (null != prop)
            {
                object dataFromModel = prop.GetValue(data, null);
                return dataFromModel;
            }

            return null;
        }

        private void SetPropertyValue(Object source, Object target, string field, Object newFieldValue)
        {
            PropertyInfo prop = target.GetType().GetProperty(field);
            if (null != prop)
            {
                prop.SetValue(target, newFieldValue, null);

            }
        }
    }
}
