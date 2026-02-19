namespace VectraCompiler.Emit.Models;

public enum Opcode : byte
{
    NOP = 0X00,
    POP = 0X01,
    DUP = 0X02,
    
    LOAD_LOCAL = 0X10,
    STORE_LOCAL = 0X11,
    
    LOAD_CONST = 0X20,
    LOAD_NULL = 0X21,
    LOAD_TRUE = 0X22,
    LOAD_FALSE = 0X23,
    
    NEW_OBJ = 0X30,
    LOAD_MEMBER = 0X31,
    STORE_MEMBER = 0X32,
    
    CALL = 0X40,
    CALL_CTOR = 0X41,
    RET = 0X42,
    CALL_NATIVE = 0X43,
    
    ADD = 0X50,
    SUB = 0X51,
    MUL = 0X52,
    DIV = 0X53,
    MOD = 0X54,
    NEG = 0X55,
    
    CEQ = 0X60,
    CNE = 0X61,
    CLT = 0X62,
    CLE = 0X63,
    CGT = 0X64,
    CGE = 0X65,
    
    JMP = 0X70,
    JMP_TRUE = 0X71,
    JMP_FALSE = 0X72,
}

public enum ConstantKind : byte
{
    Type = 0x01,
    Constructor = 0x02,
    Method = 0x03,
    Field = 0x04,
    Property = 0x05,
    String = 0x06,
    Number = 0x07
}