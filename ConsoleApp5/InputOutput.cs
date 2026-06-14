using System;
using System.Collections.Generic;
using System.IO;

namespace Компилятор
{
    public struct TextPosition
    {
        private uint _lineNumber;
        private byte _charNumber;

        public uint LineNumber
        {
            get => _lineNumber;
            set => _lineNumber = value;
        }

        public byte CharNumber
        {
            get => _charNumber;
            set => _charNumber = value;
        }

        public TextPosition(uint ln = 0, byte c = 0)
        {
            _lineNumber = ln;
            _charNumber = c;
        }
    }

    public struct Err
    {
        private TextPosition _errorPosition;
        private byte _errorCode;

        public TextPosition ErrorPosition
        {
            get => _errorPosition;
            set => _errorPosition = value;
        }

        public byte ErrorCode
        {
            get => _errorCode;
            set => _errorCode = value;
        }

        public Err(TextPosition pos, byte code)
        {
            _errorPosition = pos;
            _errorCode = code;
        }
    }

    public static class InputOutput
    {
        private const byte ERRMAX = 9;
        private static string[] allLines;
        private static int currentLineIndex;
        private static uint errCount;
        private static TextPosition _positionNow;
        private static List<Err> errors;
        private static int currentPosInLine;
        private static bool endOfFileReached;

        public static char Ch { get; private set; }

        public static TextPosition positionNow
        {
            get => _positionNow;
            private set => _positionNow = value;
        }

        public static List<Err> err => errors;

        public static void Init(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"Файл {fileName} не найден.");
            }

            allLines = File.ReadAllLines(fileName);
            currentLineIndex = 0;
            _positionNow = new TextPosition(1, 0);
            errors = new List<Err>();
            errCount = 0;
            currentPosInLine = 0;
            endOfFileReached = false;

            // Пропускаем пустые строки в начале
            SkipEmptyLines();

            if (currentLineIndex >= allLines.Length)
            {
                Ch = '\0';
                endOfFileReached = true;
            }
            else
            {
                Ch = allLines[currentLineIndex][0];
            }
        }

        private static void SkipEmptyLines()
        {
            while (currentLineIndex < allLines.Length && allLines[currentLineIndex].Length == 0)
            {
                currentLineIndex++;
                if (currentLineIndex < allLines.Length)
                {
                    _positionNow = new TextPosition(_positionNow.LineNumber + 1, 0);
                }
            }
        }

        public static void Close()
        {
            Console.WriteLine($"\nКомпиляция завершена: ошибок — {errCount}!");
        }

        private static void ListThisLine()
        {
            if (currentLineIndex >= 0 && currentLineIndex < allLines.Length)
            {
                Console.WriteLine(allLines[currentLineIndex]);
            }
        }

        private static void ListErrors()
        {
            int pos = 6 - $"{_positionNow.LineNumber} ".Length;
            foreach (Err e in errors)
            {
                errCount++;
                string s = "**";
                if (errCount < 10)
                    s += "0";
                s += $"{errCount}**";
                while (s.Length - 1 < pos + e.ErrorPosition.CharNumber)
                    s += " ";
                string errorMsg = Errors.GetMessage(e.ErrorCode);
                s += $"^ ошибка код {e.ErrorCode}: {errorMsg}";
                Console.WriteLine(s);
            }
            errors.Clear();
        }

        public static void NextCh()
        {
            if (endOfFileReached)
            {
                Ch = '\0';
                return;
            }

            // Переходим к следующему символу в текущей строке
            currentPosInLine++;
            string currentLine = allLines[currentLineIndex];

            // Проверяем, достигли ли конца текущей строки
            if (currentPosInLine >= currentLine.Length)
            {
                // Выводим строку, которую только что закончили обрабатывать
                ListThisLine();
                if (errors.Count > 0)
                {
                    ListErrors();
                }

                // Переходим к следующей строке
                currentLineIndex++;

                // Пропускаем все последующие пустые строки
                while (currentLineIndex < allLines.Length && allLines[currentLineIndex].Length == 0)
                {
                    // Выводим пустые строки
                    Console.WriteLine(allLines[currentLineIndex]);
                    currentLineIndex++;
                    _positionNow = new TextPosition(_positionNow.LineNumber + 1, 0);
                }

                // Проверяем, не достигли ли конца файла
                if (currentLineIndex >= allLines.Length)
                {
                    endOfFileReached = true;
                    Ch = '\0';
                    return;
                }

                // Начинаем новую строку
                currentPosInLine = 0;
                _positionNow = new TextPosition(_positionNow.LineNumber + 1, 0);
                Ch = allLines[currentLineIndex][0];
            }
            else
            {
                // Продолжаем в текущей строке
                _positionNow = new TextPosition(_positionNow.LineNumber, (byte)currentPosInLine);
                Ch = currentLine[currentPosInLine];
            }
        }

        public static void Error(byte errorCode, TextPosition position)
        {
            if (errors.Count <= ERRMAX)
                errors.Add(new Err(position, errorCode));
        }

        public static void SetCharNumber(byte newCharNumber)
        {
            if (endOfFileReached || currentLineIndex >= allLines.Length) return;

            currentPosInLine = newCharNumber;
            _positionNow = new TextPosition(_positionNow.LineNumber, newCharNumber);

            string currentLine = allLines[currentLineIndex];
            if (currentPosInLine < currentLine.Length)
                Ch = currentLine[currentPosInLine];
            else
                Ch = '\n';
        }

        public static void IncrementCharNumber()
        {
            currentPosInLine++;
            _positionNow = new TextPosition(_positionNow.LineNumber, (byte)currentPosInLine);

            if (!endOfFileReached && currentLineIndex < allLines.Length)
            {
                string currentLine = allLines[currentLineIndex];
                if (currentPosInLine < currentLine.Length)
                    Ch = currentLine[currentPosInLine];
            }
        }

        public static void DecrementCharNumber()
        {
            if (currentPosInLine > 0)
            {
                currentPosInLine--;
                _positionNow = new TextPosition(_positionNow.LineNumber, (byte)currentPosInLine);

                if (!endOfFileReached && currentLineIndex < allLines.Length)
                {
                    string currentLine = allLines[currentLineIndex];
                    if (currentPosInLine < currentLine.Length)
                        Ch = currentLine[currentPosInLine];
                }
            }
        }

        public static void DebugPrintState()
        {
            Console.WriteLine($"[DEBUG] lineIndex={currentLineIndex}, posInLine={currentPosInLine}, lineCount={allLines.Length}, endOfFile={endOfFileReached}, Ch='{Ch}'");
        }
    }
}
