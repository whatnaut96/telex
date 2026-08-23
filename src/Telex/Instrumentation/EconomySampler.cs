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
            var population = Singleton<CitizenManager>.instance;

            var data = new Dictionary<string, object>();
            if (economy != null)
            {
                CaptureSimple(economy, data, "LastCashAmount", "LastCashDelta", "m_cashAmount", "m_cashDelta", "m_taxMultiplier", "m_startMoney");
                data["tax_rates"] = TaxRateRecords(economy);
                data["service_budget_day"] = BudgetRecords(economy, false);
                data["service_budget_night"] = BudgetRecords(economy, true);
                data["income_by_resource"] = EconomyResourceMap(economy, "m_income");
                data["total_income_by_resource"] = EconomyResourceMap(economy, "m_totalIncome");
                data["expenses_by_resource"] = EconomyResourceMap(economy, "m_expenses");
                data["loan_expenses"] = EconomyResourceRecords(economy, "m_loanExpenses");
                data["policy_expenses"] = EconomyResourceRecords(economy, "m_policyExpenses");
                data["total_expenses_by_resource"] = EconomyResourceMap(economy, "m_totalExpenses");
                data["loans"] = LoanRecords(economy);
                AddCyberstatAliases(data);
            }

            if (population != null)
            {
                data["population"] = population.m_citizenCount;
            }

            return data;
        }

        private static void AddCyberstatAliases(IDictionary<string, object> data)
        {
            data["balance"] = Get(data, "cash_amount");
            data["income_tax_residential"] = null;
            data["income_tax_commercial"] = null;
            data["income_tax_industrial"] = null;
            data["income_tax_office"] = null;
            data["income_government_subsidy"] = null;
            data["expense_service_upkeep"] = SumMap(data["expenses_by_resource"] as IDictionary<string, object>);
            data["expense_loan_interest"] = SumRecords(data["loan_expenses"] as IList<object>);
            data["expense_subsidy_commercial"] = null;
            data["expense_subsidy_industrial"] = null;
            data["expense_subsidy_office"] = null;
            data["expense_subsidy_residential"] = null;
            data["tax_rates_residential"] = ResidentialTaxRates();
        }

        private static object ResidentialTaxRates()
        {
            var economy = Singleton<EconomyManager>.instance;
            var data = new Dictionary<string, object>();
            if (economy == null)
            {
                return data;
            }

            data["low_density"] = economy.GetTaxRate(ItemClass.Service.Residential, ItemClass.SubService.ResidentialLow, ItemClass.Level.Level1);
            data["high_density"] = economy.GetTaxRate(ItemClass.Service.Residential, ItemClass.SubService.ResidentialHigh, ItemClass.Level.Level1);
            return data;
        }

        private static object SumRecords(IList<object> records)
        {
            if (records == null)
            {
                return null;
            }

            long total = 0;
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i] as IDictionary<string, object>;
                if (record != null && record.ContainsKey("value") && record["value"] != null)
                {
                    total += System.Convert.ToInt64(record["value"]);
                }
            }

            return total;
        }

        private static object SumMap(IDictionary<string, object> values)
        {
            if (values == null)
            {
                return null;
            }

            long total = 0;
            foreach (var value in values.Values)
            {
                if (value != null)
                {
                    total += System.Convert.ToInt64(value);
                }
            }

            return total;
        }

        private static object Get(IDictionary<string, object> data, string key)
        {
            return data.ContainsKey(key) ? data[key] : null;
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
                records[key] = values.GetValue(i);
            }

            return records;
        }

        private static IList<object> TaxRateRecords(EconomyManager economy)
        {
            var records = new List<object>();
            AddTaxRates(economy, records, ItemClass.Service.Residential, ItemClass.SubService.ResidentialLow);
            AddTaxRates(economy, records, ItemClass.Service.Residential, ItemClass.SubService.ResidentialHigh);
            AddTaxRates(economy, records, ItemClass.Service.Residential, ItemClass.SubService.ResidentialLowEco);
            AddTaxRates(economy, records, ItemClass.Service.Residential, ItemClass.SubService.ResidentialHighEco);
            AddTaxRates(economy, records, ItemClass.Service.Residential, ItemClass.SubService.ResidentialWallToWall);
            AddTaxRates(economy, records, ItemClass.Service.Commercial, ItemClass.SubService.CommercialLow);
            AddTaxRates(economy, records, ItemClass.Service.Commercial, ItemClass.SubService.CommercialHigh);
            AddTaxRates(economy, records, ItemClass.Service.Commercial, ItemClass.SubService.CommercialLeisure);
            AddTaxRates(economy, records, ItemClass.Service.Commercial, ItemClass.SubService.CommercialTourist);
            AddTaxRates(economy, records, ItemClass.Service.Commercial, ItemClass.SubService.CommercialEco);
            AddTaxRates(economy, records, ItemClass.Service.Commercial, ItemClass.SubService.CommercialWallToWall);
            AddTaxRates(economy, records, ItemClass.Service.Industrial, ItemClass.SubService.IndustrialGeneric);
            AddTaxRates(economy, records, ItemClass.Service.Industrial, ItemClass.SubService.IndustrialForestry);
            AddTaxRates(economy, records, ItemClass.Service.Industrial, ItemClass.SubService.IndustrialFarming);
            AddTaxRates(economy, records, ItemClass.Service.Industrial, ItemClass.SubService.IndustrialOil);
            AddTaxRates(economy, records, ItemClass.Service.Industrial, ItemClass.SubService.IndustrialOre);
            AddTaxRates(economy, records, ItemClass.Service.Office, ItemClass.SubService.OfficeGeneric);
            AddTaxRates(economy, records, ItemClass.Service.Office, ItemClass.SubService.OfficeHightech);
            AddTaxRates(economy, records, ItemClass.Service.Office, ItemClass.SubService.OfficeWallToWall);
            AddTaxRates(economy, records, ItemClass.Service.Office, ItemClass.SubService.OfficeFinancial);
            return records;
        }

        private static void AddTaxRates(EconomyManager economy, IList<object> records, ItemClass.Service service, ItemClass.SubService subService)
        {
            AddTaxRate(economy, records, service, subService, ItemClass.Level.Level1);
            AddTaxRate(economy, records, service, subService, ItemClass.Level.Level2);
            AddTaxRate(economy, records, service, subService, ItemClass.Level.Level3);
            AddTaxRate(economy, records, service, subService, ItemClass.Level.Level4);
            AddTaxRate(economy, records, service, subService, ItemClass.Level.Level5);
        }

        private static void AddTaxRate(EconomyManager economy, IList<object> records, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            var serviceName = service.ToString();
            var subServiceName = subService.ToString();
            var levelName = level.ToString();
            var record = new Dictionary<string, object>();
            record["key"] = Normalize(serviceName) + "." + Normalize(subServiceName) + "." + Normalize(levelName);
            record["service"] = serviceName;
            record["sub_service"] = subServiceName;
            record["level"] = levelName;
            record["value"] = economy.GetTaxRate(service, subService, level);
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
            var subServiceName = subService.ToString();
            var record = new Dictionary<string, object>();
            record["key"] = Normalize(serviceName) + "." + Normalize(subServiceName);
            record["service"] = serviceName;
            record["sub_service"] = subServiceName;
            record["period"] = night ? "night" : "day";
            record["value"] = economy.GetBudget(service, subService, night);
            records.Add(record);
        }

        private static IList<object> ArrayRecords(object source, string fieldName)
        {
            var values = GetArray(source, fieldName);
            var records = new List<object>();
            if (values == null)
            {
                return records;
            }

            for (var i = 0; i < values.Length; i++)
            {
                var record = new Dictionary<string, object>();
                record["index"] = i;
                record["value"] = values.GetValue(i);
                records.Add(record);
            }

            return records;
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

                target[Normalize(name)] = ConvertValue(field.GetValue(source));
            }
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
