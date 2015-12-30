using System;
using Microsoft.FSharp.Core;
using FParsec;

namespace LuaCP.Parser.Extensions
{
    public sealed class NamedReference<TResult, TUserState> : FSharpFunc<CharStream<TUserState>, Reply<TResult>>
    {
        private readonly string name;
        public string Name { get { return name; } }

        private ErrorMessageList error;

        public NamedReference(string name)
        {
            this.name = name;
            error = new ErrorMessageList(new ErrorMessage.Expected(name));
        }

        public NamedReference(string name, FSharpFunc<CharStream<TUserState>, Reply<TResult>> parser)
            : this(name)
        {
            Parser = parser;
        }

        public FSharpFunc<CharStream<TUserState>, Reply<TResult>> Parser { get; set; }

        public FSharpFunc<CharStream<TUserState>, Reply<TResult>> AsParser { get { return this; } }

        public override Reply<TResult> Invoke(CharStream<TUserState> stream)
        {
            long stateTag = stream.StateTag;
            Reply<TResult> reply = Parser.Invoke(stream);
            if (stateTag == stream.StateTag)
            {
                reply.Error = error;
            }
            return reply;
        }
    }
}

