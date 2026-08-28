using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;

namespace Telex.Instrumentation
{
    internal static class EconomySampler
    {
        public static object Sample()
        {
            var economy = Singleton<EconomyManager>.instance;

            var data = new Dictionary<string, object>();
            if (economy != null)
            {
                CaptureSimple(economy, data, "LastCashAmount", "m_cashAmount", "m_taxMultiplier", "m_startMoney");
                data["tax_rates"] = TaxRateRecords(economy);
                data["service_budget_day"] = BudgetRecords(economy, false);
                data["service_budget_night"] = BudgetRecords(economy, true);
                data["income_by_resource"] = EconomyResourceMap(economy, "m_income");
                data["total_income_by_resource"] = EconomyResourceMap(economy, "m_totalIncome");
                data["expenses_by_resource"] = EconomyResourceMap(economy, "m_expenses");
                data["loan_expenses"] = EconomyResourceRecords(economy, "m_loanExpenses");
                data["policy_expenses"] = EconomyResourceRecords(economy, "m_policyExpenses");
                data["total_expenses_by_resource"] = EconomyResourceMap(economy, "m_totalExpenses");
            }

            return data;
        }

        private static IList<object> EconomyResourceRecords(object source, string fieldName)
        {
            var values = GetArray(source, fieldName);
            var records = new List<object>();
            if (values == null)
            {
                return records;
            }

            var names = Enum.GetNames(typeof(EconomyManager.Resource));
            for (var i = 0; i < values.Length; i++)
            {
                if (i >= names.Length)
                {
                    continue;
                }

                var record = new Dictionary<string, object>();
                record["index"] = i;
                record["name"] = names[i];
                record["value"] = values.GetValue(i);
                records.Add(record);
            }

            return records;
        }

        private static object EconomyResourceMap(object source, string fieldName)
        {
            var values = GetArray(source, fieldName);
            var records = new Dictionary<string, object>();
            if (values == null)
            {
                return records;
            }

            var names = Enum.GetNames(typeof(EconomyManager.Resource));
            for (var i = 0; i < values.Length; i++)
            {
                if (i >= names.Length)
                {
                    continue;
                }

                var key = Normalize(names[i]);
                records[key] = ConvertMoney(values.GetValue(i));
            }

            return records;
        }

        private static IList<object> TaxRateRecords(EconomyManager economy)
        {
            var records = new List<object>();
            AddTaxRate(economy, records, "residential_low", ItemClass.Service.Residential, ItemClass.SubService.ResidentialLow);
            AddTaxRate(economy, records, "residential_high", ItemClass.Service.Residential, ItemClass.SubService.ResidentialHigh);
            AddTaxRate(economy, records, "commercial_low", ItemClass.Service.Commercial, ItemClass.SubService.CommercialLow);
            AddTaxRate(economy, records, "commercial_high", ItemClass.Service.Commercial, ItemClass.SubService.CommercialHigh);
            AddTaxRate(economy, records, "office", ItemClass.Service.Office, ItemClass.SubService.OfficeGeneric);
            AddTaxRate(economy, records, "industry", ItemClass.Service.Industrial, ItemClass.SubService.IndustrialGeneric);
            return records;
        }

        private static void AddTaxRate(EconomyManager economy, IList<object> records, string category, ItemClass.Service service, ItemClass.SubService subService)
        {
            var record = new Dictionary<string, object>();
            record["category"] = category;
            record["service"] = Normalize(service.ToString());
            record["sub_service"] = SubServiceName(subService);
            record["value"] = economy.GetTaxRate(service, subService, ItemClass.Level.Level1);
            records.Add(record);
        }

        private static IList<object> BudgetRecords(EconomyManager economy, bool night)
        {
            var records = new List<object>();
            AddBudget(records, economy, ItemClass.Service.Road, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.Electricity, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.Water, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.Garbage, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.HealthCare, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.PoliceDepartment, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.PoliceDepartment, ItemClass.SubService.PoliceDepartmentBank, night);
            AddBudget(records, economy, ItemClass.Service.Education, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.FireDepartment, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.Beautification, ItemClass.SubService.BeautificationParks, night);
            AddBudget(records, economy, ItemClass.Service.Disaster, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.PlayerIndustry, ItemClass.SubService.PlayerIndustryForestry, night);
            AddBudget(records, economy, ItemClass.Service.PlayerIndustry, ItemClass.SubService.PlayerIndustryFarming, night);
            AddBudget(records, economy, ItemClass.Service.PlayerIndustry, ItemClass.SubService.PlayerIndustryOil, night);
            AddBudget(records, economy, ItemClass.Service.PlayerIndustry, ItemClass.SubService.PlayerIndustryOre, night);
            AddBudget(records, economy, ItemClass.Service.PlayerEducation, ItemClass.SubService.PlayerEducationTradeSchool, night);
            AddBudget(records, economy, ItemClass.Service.PlayerEducation, ItemClass.SubService.PlayerEducationLiberalArts, night);
            AddBudget(records, economy, ItemClass.Service.PlayerEducation, ItemClass.SubService.PlayerEducationUniversity, night);
            AddBudget(records, economy, ItemClass.Service.Museums, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.VarsitySports, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.Fishing, ItemClass.SubService.None, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportBus, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportMetro, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportTrain, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportShip, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportPlane, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportTaxi, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportTram, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportMonorail, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportCableCar, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportTours, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportPost, night);
            AddBudget(records, economy, ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportTrolleybus, night);
            return records;
        }

        private static void AddBudget(IList<object> records, EconomyManager economy, ItemClass.Service service, ItemClass.SubService subService, bool night)
        {
            var serviceName = service.ToString();
            var record = new Dictionary<string, object>();
            record["service"] = Normalize(serviceName);
            record["sub_service"] = SubServiceName(subService);
            record["period"] = night ? "night" : "day";
            record["value"] = economy.GetBudget(service, subService, night);
            records.Add(record);
        }

        private static string SubServiceName(ItemClass.SubService subService)
        {
            return subService == ItemClass.SubService.None ? "none" : Normalize(subService.ToString());
        }

        private static IList<object> LoanRecords(object source)
        {
            var loans = GetArray(source, "m_loans");
            var records = new List<object>();
            if (loans == null)
            {
                return records;
            }

            for (var i = 0; i < loans.Length; i++)
            {
                var loan = loans.GetValue(i);
                var record = new Dictionary<string, object>();
                record["index"] = i;
                CaptureSimple(loan, record, "m_amountTaken", "m_amountLeft", "m_interestRate", "m_interestPaid", "m_length");
                records.Add(record);
            }

            return records;
        }

        private static Array GetArray(object source, string fieldName)
        {
            if (source == null)
            {
                return null;
            }

            var field = source.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field == null ? null : field.GetValue(source) as Array;
        }

        private static void CaptureSimple(object source, IDictionary<string, object> target, params string[] names)
        {
            if (source == null)
            {
                return;
            }

            var type = source.GetType();
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    continue;
                }

                var key = Normalize(name);
                var value = field.GetValue(source);
                target[key] = IsMoneyField(key) ? ConvertMoney(value) : ConvertValue(value);
            }
        }

        private static bool IsMoneyField(string key)
        {
            return key == "last_cash_amount" || key == "cash_amount" || key == "start_money";
        }

        private static object ConvertMoney(object value)
        {
            if (value == null)
            {
                return null;
            }

            return System.Convert.ToDouble(value) / 100d;
        }

        private static object ConvertValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is IEnumerable && !(value is string))
            {
                return null;
            }

            return value.GetType().IsEnum ? value.ToString() : value;
        }

        private static string Normalize(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (name.StartsWith("m_"))
            {
                name = name.Substring(2);
            }

            var chars = new List<char>();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c) && chars.Count > 0 && chars[chars.Count - 1] != '_')
                {
                    chars.Add('_');
                }
                chars.Add(char.ToLowerInvariant(c));
            }

            return new string(chars.ToArray());
        }
    }
}
