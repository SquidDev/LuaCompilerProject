namespace LuaCP.CodeGen.Bytecode
{
	public enum LuaOpcode
	{
		/// <summary>
		/// A B     R(A) := R(B)
		/// </summary>
		Move,
		/// <summary>
		/// A Bx    R(A) := Kst(Bx)
		/// </summary>
		LoadK,
		/// <summary>
		/// A       R(A) := Kst(extra arg)
		/// </summary>
		Loadkx,
		/// <summary>
		/// A B C   R(A) := (Bool)B; if (C) pc++
		/// </summary>
		LoadBool,
		/// <summary>
		/// A B     R(A), R(A+1), ..., R(A+B) := nil
		/// </summary>
		LoadNil,

		/// <summary>
		/// A B     R(A) := UpValue[B]
		/// </summary>
		GetUpval,
		/// <summary>
		/// A B C   R(A) := UpValue[B][RK(C)]
		/// </summary>
		GetTabUp,
		/// <summary>
		/// A B C   R(A) := R(B)[RK(C)]
		/// </summary>
		GetTable,

		/// <summary>
		/// A B C   UpValue[A][RK(B)] := RK(C)
		/// </summary>
		SetTabUp,
		/// <summary>
		/// A B     UpValue[B] := R(A)
		/// </summary>
		SetUpval,
		/// <summary>
		/// A B C   R(A)[RK(B)] := RK(C)
		/// </summary>
		SetTable,

		/// <summary>
		/// A B C   R(A) := {} (size = B,C)
		/// </summary>
		Newtable,

		/// <summary>
		/// A B C   R(A+1) := R(B); R(A) := R(B)[RK(C)]
		/// </summary>
		Self,

		/// <summary>
		/// A B C   R(A) := RK(B) + RK(C)
		/// </summary>
		Add,
		/// <summary>
		/// A B C   R(A) := RK(B) - RK(C)
		/// </summary>
		Sub,
		/// <summary>
		/// A B C   R(A) := RK(B) * RK(C)
		/// </summary>
		Mul,
		/// <summary>
		/// A B C   R(A) := RK(B) % RK(C)
		/// </summary>
		Mod,
		/// <summary>
		/// A B C   R(A) := RK(B) ^ RK(C)
		/// </summary>
		Pow,
		/// <summary>
		/// A B C   R(A) := RK(B) / RK(C)
		/// </summary>
		Div,
		/// <summary>
		/// A B C   R(A) := RK(B) // RK(C)
		/// </summary>
		Idiv,
		/// <summary>
		/// A B C   R(A) := RK(B) & RK(C)
		/// </summary>
		Band,
		/// <summary>
		/// A B C   R(A) := RK(B) | RK(C)
		/// </summary>
		Bor,
		/// <summary>
		/// A B C   R(A) := RK(B) ~ RK(C)
		/// </summary>
		Bxor,
		/// <summary>
		/// A B C   R(A) := RK(B) << RK(C)
		/// </summary>
		Shl,
		/// <summary>
		/// A B C   R(A) := RK(B) >> RK(C)
		/// </summary>
		Shr,
		/// <summary>
		/// A B     R(A) := -R(B)
		/// </summary>
		Unm,
		/// <summary>
		/// A B     R(A) := ~R(B)
		/// </summary>
		Bnot,
		/// <summary>
		/// A B     R(A) := not R(B)
		/// </summary>
		Not,
		/// <summary>
		/// A B     R(A) := length of R(B)
		/// </summary>
		Len,

		/// <summary>
		/// A B C   R(A) := R(B).. ... ..R(C)
		/// </summary>
		Concat,

		/// <summary>
		/// A sBx   pc+=sBx; if (A) close all upvalues >= R(A - 1)
		/// </summary>
		Jmp,
		/// <summary>
		/// A B C   if ((RK(B) == RK(C)) ~= A) then pc++
		/// </summary>
		Eq,
		/// <summary>
		/// A B C   if ((RK(B) <  RK(C)) ~= A) then pc++
		/// </summary>
		Lt,
		/// <summary>
		/// A B C   if ((RK(B) <= RK(C)) ~= A) then pc++
		/// </summary>
		Le,

		/// <summary>
		/// A C     if not (R(A) <=> C) then pc++
		/// </summary>
		Test,
		/// <summary>
		/// A B C   if (R(B) <=> C) then R(A) := R(B) else pc++
		/// </summary>
		TestSet,

		/// <summary>
		/// A B C   R(A), ... ,R(A+C-2) := R(A)(R(A+1), ... ,R(A+B-1))
		/// </summary>
		Call,
		/// <summary>
		/// A B C   return R(A)(R(A+1), ... ,R(A+B-1))
		/// </summary>
		TailCall,
		/// <summary>
		/// A B     return R(A), ... ,R(A+B-2)      (see note)
		/// </summary>
		Return,

		/// <summary>
		/// A sBx   R(A)+=R(A+2); if R(A) <?= R(A+1) then { pc+=sBx; R(A+3)=R(A) }
		/// </summary>
		ForLoop,
		/// <summary>
		/// A sBx   R(A)-=R(A+2); pc+=sBx
		/// </summary>
		ForPrep,

		/// <summary>
		/// A C     R(A+3), ... ,R(A+2+C) := R(A)(R(A+1), R(A+2));
		/// </summary>
		TForCall,
		/// <summary>
		/// A sBx   if R(A+1) ~= nil then { R(A)=R(A+1); pc += sBx }
		/// </summary>
		TForLoop,

		/// <summary>
		/// A B C   R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B
		/// </summary>
		SetList,

		/// <summary>
		/// A Bx    R(A) := closure(KPROTO[Bx])
		/// </summary>
		Closure,

		/// <summary>
		/// A B     R(A), R(A+1), ..., R(A+B-2) = vararg
		/// </summary>
		Vararg,

		/// <summary>
		/// Ax      extra (larger) argument for previous opcode
		/// </summary>
		ExtraArg,
	}
}

