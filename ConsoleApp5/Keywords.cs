using System;
using System.Collections.Generic;

namespace Компилятор
{
    public static class Keywords
    {
        private static readonly Dictionary<string, byte> keywordTable;

        static Keywords()
        {
            keywordTable = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            InitializeKeywords();
        }

        private static void InitializeKeywords()
        {
            AddKeyword("program", 29);
            AddKeyword("begin", 13);
            AddKeyword("end", 14);
            AddKeyword("var", 32);
            AddKeyword("const", 34);
            AddKeyword("type", 33);
            AddKeyword("label", 35);
            AddKeyword("if", 15);
            AddKeyword("then", 16);
            AddKeyword("else", 17);
            AddKeyword("case", 23);
            AddKeyword("of", 24);
            AddKeyword("while", 18);
            AddKeyword("do", 19);
            AddKeyword("for", 20);
            AddKeyword("to", 21);
            AddKeyword("downto", 22);
            AddKeyword("repeat", 25);
            AddKeyword("until", 26);
            AddKeyword("procedure", 31);
            AddKeyword("function", 30);
            AddKeyword("and", 107);
            AddKeyword("or", 102);
            AddKeyword("not", 108);
            AddKeyword("div", 106);
            AddKeyword("mod", 110);
            AddKeyword("in", 100);
            AddKeyword("array", 36);
            AddKeyword("record", 38);
            AddKeyword("file", 39);
            AddKeyword("set", 40);
            AddKeyword("packed", 37);
            AddKeyword("goto", 28);
            AddKeyword("with", 27);
            AddKeyword("nil", 111);
        }

        private static void AddKeyword(string word, byte code)
        {
            if (!keywordTable.ContainsKey(word))
                keywordTable.Add(word, code);
        }

        public static bool IsKeyword(string word, out byte code)
        {
            return keywordTable.TryGetValue(word, out code);
        }

        public static bool IsKeyword(string word)
        {
            return keywordTable.ContainsKey(word);
        }

        public static byte GetKeywordCode(string word)
        {
            return keywordTable.TryGetValue(word, out byte code) ? code : (byte)0;
        }

        public static IEnumerable<string> GetAllKeywords()
        {
            return keywordTable.Keys;
        }

        public static IReadOnlyDictionary<string, byte> GetKeywordTable()
        {
            return keywordTable;
        }

        public static void PrintKeywords()
        {
            Console.WriteLine("Таблица ключевых слов Паскаля:");
            Console.WriteLine(new string('-', 40));
            Console.WriteLine($"{"КЛЮЧЕВОЕ СЛОВО",-20} | {"КОД",8}");
            Console.WriteLine(new string('-', 40));

            foreach (var kvp in keywordTable)
                Console.WriteLine($"{kvp.Key,-20} | {kvp.Value,8}");

            Console.WriteLine(new string('-', 40));
            Console.WriteLine($"Всего ключевых слов: {keywordTable.Count}");
        }
    }
}
