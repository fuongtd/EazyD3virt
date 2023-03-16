﻿using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.PatternMatching.Patterns.OpCodes;

internal record Ldstr : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance string VM::ResolveString(int32)
        CilOpCodes.Stloc_1,     // 7	0013	stloc.1
        CilOpCodes.Ldarg_0,     // 8	0014	ldarg.0
        CilOpCodes.Newobj,      // 9	0015	newobj	instance void VMStringOperand::.ctor()
        CilOpCodes.Dup,         // 10	001A	dup
        CilOpCodes.Ldloc_1,     // 11	001B	ldloc.1
        CilOpCodes.Callvirt,    // 12	001C	callvirt	instance void VMStringOperand::method_4(string)
        CilOpCodes.Callvirt,    // 13	0021	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 14	0026	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldstr;

    public bool Verify(VMOpCode vmOpCode, int index) => (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[6].Operand as SerializedMethodDefinition)!.Signature!.ReturnType.FullName 
                                                        == "System.String";
}

#region Return
internal record EnableReturnFromVMMethodPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Ldc_I4_1,    // 1	0001	ldc.i4.1
        CilOpCodes.Stfld,       // 2	0002	stfld	bool VM::ReturnFromVMMethod
        CilOpCodes.Ret          // 3	0007	ret
    };
}

internal record Ret : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance void VM::EnableReturnFromVMMethod()
        CilOpCodes.Ret          // 2	0006	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ret;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new EnableReturnFromVMMethodPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand as SerializedMethodDefinition)!);
}
#endregion Return

#region Ldnull
internal record Ldnull : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Newobj,      // 1	0001	newobj	instance void VMObjectOperand::.ctor()
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 3	000B	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldnull;

    public bool Verify(VMOpCode vmOpCode, int index)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions;
        var vmObjectOperandCtor = instructions[1].Operand as SerializedMethodDefinition;
        if (vmObjectOperandCtor?.Name != ".ctor") return false;

        var declaringType = vmObjectOperandCtor.DeclaringType!;
        if (declaringType.Fields.Count != 1 ||
            declaringType.Fields[0].Signature!.FieldType.FullName != "System.Object") return false;
        
        var baseType = declaringType.BaseType?.Resolve();
        return baseType is { Fields.Count: 2 } && baseType.Fields.Count(f => f.Signature!.FieldType.FullName == "System.Int32") == 1 || baseType!.Fields.Count(f => f.Signature!.FieldType.FullName == "System.Type") == 1;
    }
}
#endregion Ldnull

internal record Dup : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class VMOperandType VM::PeekStack()
        CilOpCodes.Stloc_0,     // 2	0006	stloc.0
        CilOpCodes.Ldarg_0,     // 3	0007	ldarg.0
        CilOpCodes.Ldloc_0,     // 4	0008	ldloc.0
        CilOpCodes.Callvirt,    // 5	0009	callvirt	instance class VMOperandType VMOperandType::vmethod_3()
        CilOpCodes.Callvirt,    // 6	000E	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 7	0013	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Dup;

    public bool Verify(VMOpCode vmOpCode, int index) =>
        PatternMatcher.MatchesPattern(new PeekStackPattern(),
            (vmOpCode.SerializedDelegateMethod.CilMethodBody!.Instructions[1].Operand as SerializedMethodDefinition)!);
}

#region Pop

internal record Pop : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 0	0000	ldarg.0
        CilOpCodes.Callvirt,    // 1	0001	callvirt	instance class Class20 VM::PopStack()
        CilOpCodes.Pop,         // 2	0006	pop
        CilOpCodes.Ret          // 3	0007	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Pop;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions;
        return instructions![2].OpCode == CilOpCodes.Pop && PatternMatcher.GetAllMatchingInstructions(
            new PopStackPattern(),
            (instructions[1].Operand as SerializedMethodDefinition)!).Count == 1;
    }
}
#endregion Pop

internal record Ldtoken : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_0,     // 33	0064	ldarg.0
        CilOpCodes.Ldloc_0,     // 34	0065	ldloc.0
        CilOpCodes.Callvirt,    // 35	0066	callvirt	instance class [mscorlib]System.Reflection.FieldInfo VM::ResolveFieldCache(int32)
        CilOpCodes.Callvirt,    // 36	006B	callvirt	instance valuetype [mscorlib]System.RuntimeFieldHandle [mscorlib]System.Reflection.FieldInfo::get_FieldHandle()
        CilOpCodes.Box,         // 37	0070	box	[mscorlib]System.RuntimeFieldHandle
        CilOpCodes.Stloc_2,     // 38	0075	stloc.2
    };

    public CilOpCode? CilOpCode => CilOpCodes.Ldtoken;

    public bool MatchEntireBody => false;
    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions!;
        var resolveFieldCall = instructions[index + 2].Operand as SerializedMethodDefinition;
        if (!resolveFieldCall!.Signature!.ReturnsValue ||
            resolveFieldCall.Signature.ReturnType.FullName != "System.Reflection.FieldInfo")
            return false;
        
        var getFieldHandleCall = instructions[index + 3].Operand as SerializedMemberReference;
        return getFieldHandleCall!.FullName == "System.RuntimeFieldHandle System.Reflection.FieldInfo::get_FieldHandle()";
    }
}

internal record Box : IOpCodePattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldarg_1,     // 0	0000	ldarg.1
        CilOpCodes.Castclass,   // 1	0001	castclass	VMIntOperand
        CilOpCodes.Callvirt,    // 2	0006	callvirt	instance int32 VMIntOperand::method_3()
        CilOpCodes.Stloc_0,     // 3	000B	stloc.0
        CilOpCodes.Ldarg_0,     // 4	000C	ldarg.0
        CilOpCodes.Ldloc_0,     // 5	000D	ldloc.0
        CilOpCodes.Ldc_I4_1,    // 6	000E	ldc.i4.1
        CilOpCodes.Callvirt,    // 7	000F	callvirt	instance class [mscorlib]System.Type VM::ResolveVMTypeCodeCache(int32, bool)
        CilOpCodes.Stloc_1,     // 8	0014	stloc.1
        CilOpCodes.Ldarg_0,     // 9	0015	ldarg.0
        CilOpCodes.Callvirt,    // 10	0016	callvirt	instance class VMOperandType VM::PopStack()
        CilOpCodes.Callvirt,    // 11	001B	callvirt	instance object VMOperandType::vmethod_0()
        CilOpCodes.Ldloc_1,     // 12	0020	ldloc.1
        CilOpCodes.Call,        // 13	0021	call	class VMOperandType VMOperandType::smethod_0(object, class [mscorlib]System.Type)
        CilOpCodes.Stloc_2,     // 14	0026	stloc.2
        CilOpCodes.Ldloc_2,     // 15	0027	ldloc.2
        CilOpCodes.Ldloc_1,     // 16	0028	ldloc.1
        CilOpCodes.Callvirt,    // 17	0029	callvirt	instance void VMOperandType::method_2(class [mscorlib]System.Type)
        CilOpCodes.Ldarg_0,     // 18	002E	ldarg.0
        CilOpCodes.Ldloc_2,     // 19	002F	ldloc.2
        CilOpCodes.Callvirt,    // 20	0030	callvirt	instance void VM::PushStack(class VMOperandType)
        CilOpCodes.Ret          // 21	0035	ret
    };

    public CilOpCode? CilOpCode => CilOpCodes.Box;

    public bool InterchangeLdlocOpCodes => true;
    public bool InterchangeStlocOpCodes => true;

    public bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        var instructions = vmOpCode.SerializedDelegateMethod.CilMethodBody?.Instructions!;
        var resolveTypeCall = instructions[index + 7].Operand as SerializedMethodDefinition;
        if (!resolveTypeCall!.Signature!.ReturnsValue ||
            resolveTypeCall.Signature.ReturnType.FullName != "System.Type")
            return false;

        var boxOperandCall = instructions[index + 13].Operand as SerializedMethodDefinition;
        return boxOperandCall?.Parameters.Count == 2 &&
               boxOperandCall.Parameters[0].ParameterType.FullName == "System.Object" &&
               boxOperandCall.Parameters[1].ParameterType.FullName == "System.Type";
    }
}