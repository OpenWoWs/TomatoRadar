using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace TomatoRadar.Models
{
    public enum Language
    {
        AUTO,
        EN_US,
        ZH_CN,
        RU_RU,
        JA_JP,
        ZH_TW,
    }
    public static class LanguageExt
    {
        public static Language GetLanguageByName(string name)
        {
            return name switch
            {
                "AUTO" => Language.AUTO,
                "EN_US" => Language.EN_US,
                "ZH_CN" => Language.ZH_CN,
                "RU_RU" => Language.RU_RU,
                "JA_JP" => Language.JA_JP,
                "ZH_TW" => Language.ZH_TW,
                _ => throw new ArgumentException(),
            };
        }
        public static string GetNameByLanguage(Language language)
        {
            return language switch
            {
                Language.AUTO => "AUTO",
                Language.EN_US => "EN_US",
                Language.ZH_CN => "ZH_CN",
                Language.RU_RU => "RU_RU",
                Language.JA_JP => "JA_JP",
                Language.ZH_TW => "ZH_TW",
                _ => throw new ArgumentException(),
            };
        }
        public static ResourceDictionary GetResourceDictionaryByLanguage(Language language)
        {
            return language switch
            {
                Language.AUTO => CultureInfo.CurrentUICulture.Name switch
                {
                    string s when s.StartsWith("zh-TW") || s.StartsWith("zh-Hant") => new ResourceDictionary { Source = new Uri("/Resources/Localization/zh-tw.xaml", UriKind.RelativeOrAbsolute) },
                    string s when s.StartsWith("zh") => new ResourceDictionary { Source = new Uri("/Resources/Localization/zh-cn.xaml", UriKind.RelativeOrAbsolute) },
                    string s when s.StartsWith("ja") => new ResourceDictionary { Source = new Uri("/Resources/Localization/ja-jp.xaml", UriKind.RelativeOrAbsolute) },
                    string s when s.StartsWith("ru") => new ResourceDictionary { Source = new Uri("/Resources/Localization/ru-ru.xaml", UriKind.RelativeOrAbsolute) },
                    _ => new ResourceDictionary { Source = new Uri("/Resources/Localization/en-us.xaml", UriKind.RelativeOrAbsolute) },
                },
                Language.EN_US => new ResourceDictionary { Source = new Uri("/Resources/Localization/en-us.xaml", UriKind.RelativeOrAbsolute) },
                Language.ZH_CN => new ResourceDictionary { Source = new Uri("/Resources/Localization/zh-cn.xaml", UriKind.RelativeOrAbsolute) },
                Language.RU_RU => new ResourceDictionary { Source = new Uri("/Resources/Localization/ru-ru.xaml", UriKind.RelativeOrAbsolute) },
                Language.JA_JP => new ResourceDictionary { Source = new Uri("/Resources/Localization/ja-jp.xaml", UriKind.RelativeOrAbsolute) },
                Language.ZH_TW => new ResourceDictionary { Source = new Uri("/Resources/Localization/zh-tw.xaml", UriKind.RelativeOrAbsolute) },
                _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
                {
                    "zh" => new ResourceDictionary { Source = new Uri("/Resources/Localization/zh-cn.xaml", UriKind.RelativeOrAbsolute) },
                    _ => new ResourceDictionary { Source = new Uri("/Resources/Localization/en-us.xaml", UriKind.RelativeOrAbsolute) },
                },
            };
        }

        private static Language ResolveEffectiveAppLanguage()
        {
            string appLangSetting = Properties.Settings.Default.Language;
            if (appLangSetting != "AUTO")
                return GetLanguageByName(appLangSetting);

            string cultureName = CultureInfo.CurrentUICulture.Name;
            if (cultureName.StartsWith("zh-TW") || cultureName.StartsWith("zh-Hant"))
                return Language.ZH_TW;
            if (cultureName.StartsWith("zh"))
                return Language.ZH_CN;
            if (cultureName.StartsWith("ja"))
                return Language.JA_JP;
            if (cultureName.StartsWith("ru"))
                return Language.RU_RU;
            return Language.EN_US;
        }

        public static List<Language> ParsePriority(string priority)
        {
            if (priority == "AUTO")
            {
                Language appLang = ResolveEffectiveAppLanguage();
                var list = new List<Language>();

                list.Add(appLang);

                if (appLang == Language.ZH_CN)
                    list.Add(Language.ZH_TW);
                else if (appLang == Language.ZH_TW)
                    list.Add(Language.ZH_CN);

                if (!list.Contains(Language.EN_US))
                    list.Add(Language.EN_US);

                foreach (Language lang in AllLanguages)
                {
                    if (!list.Contains(lang) && lang != Language.RU_RU)
                        list.Add(lang);
                }

                if (!list.Contains(Language.RU_RU))
                    list.Add(Language.RU_RU);

                return list;
            }
            return priority.Split(',').Select(s => GetLanguageByName(s.Trim())).ToList();
        }

        public static string FormatPriority(List<Language> priority)
        {
            return string.Join(",", priority.Select(l => GetNameByLanguage(l)));
        }

        public static readonly Language[] AllLanguages = { Language.ZH_CN, Language.EN_US, Language.RU_RU, Language.JA_JP, Language.ZH_TW };
    }
}
