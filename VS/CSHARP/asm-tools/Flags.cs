﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsmTools {


    public class FlagValue {
        private Bt _value;
        public FlagValue(Bt v) {
            _value = v;
        }
        public Bt val { get { return _value; } set { _value = value; } }
    }

    public class CarryFlag : FlagValue {
        public CarryFlag(Bt v) : base(v) { }
        public static implicit operator Bt(CarryFlag v) { return v.val; }
        public static implicit operator CarryFlag(Bt v) { return new CarryFlag(v); }
    }
    public class AuxiliaryFlag : FlagValue {
        public AuxiliaryFlag(Bt v) : base(v) { }
        public static implicit operator Bt(AuxiliaryFlag v) { return v.val; }
        public static implicit operator AuxiliaryFlag(Bt v) { return new AuxiliaryFlag(v); }
    }
    public class ZeroFlag : FlagValue {
        public ZeroFlag(Bt v) : base(v) { }
        public static implicit operator Bt(ZeroFlag v) { return v.val; }
        public static implicit operator ZeroFlag(Bt v) { return new ZeroFlag(v); }
    }
    public class SignFlag : FlagValue {
        public SignFlag(Bt v) : base(v) { }
        public static implicit operator Bt(SignFlag v) { return v.val; }
        public static implicit operator SignFlag(Bt v) { return new SignFlag(v); }
    }
    public class ParityFlag : FlagValue {
        public ParityFlag(Bt v) : base(v) { }
        public static implicit operator Bt(ParityFlag v) { return v.val; }
        public static implicit operator ParityFlag(Bt v) { return new ParityFlag(v); }
    }
    public class OverflowFlag : FlagValue {
        public OverflowFlag(Bt v) : base(v) { }
        public static implicit operator Bt(OverflowFlag v) { return v.val; }
        public static implicit operator OverflowFlag(Bt v) { return new OverflowFlag(v); }
    }

    /// <summary>
    /// Flags, CF, PF, AF, ZF, SF, OF
    /// </summary>
    public enum Flags : byte {
        /// <summary>
        /// CF (bit 0) Carry flag — Set if an arithmetic operation generates a carry
        /// or a borrow out of the most significant bit of the result; cleared otherwise.
        /// This flag indicates an overflow condition for unsigned-integer arithmetic.
        /// It is also used in multiple-precision arithmetic.
        /// </summary>
        CF = 1 << 0,
        /// <summary>
        /// PF (bit 2) Parity flag — Set if the least-significant byte of the result contains an even number of 1 bits;
        /// cleared otherwise.
        /// </summary>
        PF = 1 << 1,
        /// <summary>
        /// AF (bit 4) Auxiliary Carry flag — Set if an arithmetic operation generates a carry or a borrow out of bit
        /// 3 of the result; cleared otherwise. This flag is used in binary-coded decimal (BCD) arithmetic.
        /// </summary>
        AF = 1 << 2,
        /// <summary>
        /// ZF (bit 6) Zero flag — Set if the result is zero; cleared otherwise.
        /// </summary>
        ZF = 1 << 3,
        /// <summary>
        /// SF (bit 7) Sign flag — Set equal to the most-significant bit of the result, which is the sign bit of a signed
        /// integer. (0 indicates a positive value and 1 indicates a negative value.)
        /// </summary>
        SF = 1 << 4,
        /// <summary>
        /// OF (bit 11) Overflow flag — Set if the integer result is too large a positive number or too small a negative
        /// number (excluding the sign-bit) to fit in the destination operand; cleared otherwise. This
        /// flag indicates an overflow condition for signed-integer (two’s complement) arithmetic.
        /// </summary>
        OF = 1 << 5,

        ALL = CF | PF | AF | ZF | SF | OF
    }
}
