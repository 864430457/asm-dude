﻿// The MIT License (MIT)
//
// Copyright (c) 2017 Henk-Jan Lebbink
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

namespace AsmTools {

    [Flags]
    public enum AssemblerEnum : byte {
        UNKNOWN = 0,
        NASM    = 1 << 0,
        MASM    = 1 << 1,
        ALL     = NASM | MASM
    }

    public static partial class AsmSourceTools {
        public static AssemblerEnum parseAssembler(string str) {
            if ((str == null) || (str.Length == 0))
            {
                return AssemblerEnum.UNKNOWN;
            }
            AssemblerEnum result = AssemblerEnum.UNKNOWN;
            foreach (string str2 in str.ToUpper().Split(','))
            {
                switch (str2.Trim())
                {
                    case "MASM": result |= AssemblerEnum.MASM; break;
                    case "NASM": result |= AssemblerEnum.NASM; break;
                }
            }
            return result;
        }
    }
}
