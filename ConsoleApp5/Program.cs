using System.Text;

namespace Компилятор
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("КОМПИЛЯТОР ПАСКАЛЯ");
            Console.WriteLine("Лексический и синтаксический анализ");
            Console.WriteLine("========================================\n");

            string currentDir = Directory.GetCurrentDirectory();

            // Показываем список доступных тестовых файлов
            var pasFiles = Directory.GetFiles(currentDir, "*.pas");
            Console.WriteLine("Доступные тестовые файлы:");
            for (int i = 0; i < pasFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(pasFiles[i])}");
            }

            Console.Write("\nВыберите файл для анализа (1-" + pasFiles.Length + "): ");
            string choice = Console.ReadLine();

            string testFile;
            if (int.TryParse(choice, out int index) && index >= 1 && index <= pasFiles.Length)
            {
                testFile = pasFiles[index - 1];
            }
            else
            {
                testFile = pasFiles[0]; // По умолчанию первый файл
            }

            Console.WriteLine($"\nВыбран файл: {Path.GetFileName(testFile)}");

            try
            {
                // Лексический анализ
                Console.WriteLine("\n========================================");
                Console.WriteLine("ЛЕКСИЧЕСКИЙ АНАЛИЗ");
                Console.WriteLine("========================================\n");

                InputOutput.Init(testFile);
                LexicalAnalyzer lexer1 = new LexicalAnalyzer();

                Console.WriteLine("Содержимое файла:");
                Console.WriteLine(new string('-', 60));
                Console.WriteLine(File.ReadAllText(testFile));
                Console.WriteLine(new string('-', 60));
                Console.WriteLine("\nРезультаты лексического анализа:");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"{"№",3} | {"КОД",4} | {"ТИП ТОКЕНА",-20} | {"ЛЕКСЕМА",-20} | {"ПОЗИЦИЯ"}");
                Console.WriteLine(new string('-', 80));

                int tokenCount = 0;
                StringBuilder codesOutput = new StringBuilder();
                byte symbol;

                while (true)
                {
                    symbol = lexer1.NextToken();

                    if (symbol == 0)
                        break;

                    tokenCount++;

                    string tokenType = lexer1.GetTokenName(symbol);
                    string lexeme = GetLexeme(lexer1, symbol);
                    string position = $"стр.{lexer1.TokenPosition.LineNumber}, поз.{lexer1.TokenPosition.CharNumber}";

                    Console.WriteLine($"{tokenCount,3} | {symbol,4} | {tokenType,-20} | {lexeme,-20} | {position}");

                    codesOutput.Append(symbol).Append(' ');
                }

                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"Всего токенов: {tokenCount}");

                SaveTokenCodes(codesOutput.ToString(), "token_codes.txt");

                Console.WriteLine("\nНажмите любую клавишу для запуска синтаксического анализа...");
                Console.ReadKey();

                // Синтаксический анализ
                Console.WriteLine("\n========================================");
                Console.WriteLine("СИНТАКСИЧЕСКИЙ АНАЛИЗ");
                Console.WriteLine("========================================");

                InputOutput.Init(testFile);
                LexicalAnalyzer lexer2 = new LexicalAnalyzer();
                SyntaxAnalyzer parser = new SyntaxAnalyzer(lexer2);

                bool success = parser.ParseProgram();

                // Сохраняем результаты
                SaveAnalysisResults(success, testFile);

                InputOutput.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nНепредвиденная ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static string GetLexeme(LexicalAnalyzer lexer, byte symbol)
        {
            if (symbol == LexicalAnalyzer.IDENT)
                return lexer.IdentifierName;
            if (symbol == LexicalAnalyzer.INTCONST)
                return lexer.IntValue.ToString();
            if (symbol == LexicalAnalyzer.REALCONST)
                return lexer.FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (symbol == LexicalAnalyzer.CHARCONST)
                return $"'{lexer.CharValue}'";

            switch (symbol)
            {
                case LexicalAnalyzer.PLUS: return "+";
                case LexicalAnalyzer.MINUS: return "-";
                case LexicalAnalyzer.STAR: return "*";
                case LexicalAnalyzer.SLASH: return "/";
                case LexicalAnalyzer.EQUAL: return "=";
                case LexicalAnalyzer.ASSIGN: return ":=";
                case LexicalAnalyzer.SEMICOLON: return ";";
                case LexicalAnalyzer.COLON: return ":";
                case LexicalAnalyzer.COMMA: return ",";
                case LexicalAnalyzer.POINT: return ".";
                case LexicalAnalyzer.DOTDOT: return "..";
                case LexicalAnalyzer.LPAREN: return "(";
                case LexicalAnalyzer.RPAREN: return ")";
                case LexicalAnalyzer.LBRACKET: return "[";
                case LexicalAnalyzer.RBRACKET: return "]";
                case LexicalAnalyzer.LESS: return "<";
                case LexicalAnalyzer.GREATER: return ">";
                case LexicalAnalyzer.LESSEQUAL: return "<=";
                case LexicalAnalyzer.GREATEREQUAL: return ">=";
                case LexicalAnalyzer.NOTEQUAL: return "<>";
                case LexicalAnalyzer.CARET: return "^";
                default:
                    foreach (var kw in Keywords.GetKeywordTable())
                        if (kw.Value == symbol) return kw.Key.ToUpper();
                    return "";
            }
        }

        static void SaveTokenCodes(string codes, string filename)
        {
            try
            {
                File.WriteAllText(filename, codes);
                Console.WriteLine($"\nКоды токенов сохранены в: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
            }
        }

        static void SaveAnalysisResults(bool success, string sourceFile)
        {
            try
            {
                string resultsFile = "analysis_results.txt";
                string results = $"Анализ файла: {Path.GetFileName(sourceFile)}\n";
                results += $"Дата: {DateTime.Now}\n";
                results += $"Результат: {(success ? "УСПЕШНО" : "С ОШИБКАМИ")}\n";
                File.WriteAllText(resultsFile, results);
                Console.WriteLine($"\nРезультаты сохранены в: {resultsFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении результатов: {ex.Message}");
            }
        }
    }
}
