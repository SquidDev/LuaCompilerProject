using System;
using Microsoft.FSharp.Core;
using FParsec;
using System.Collections.Generic;

namespace LuaCP.Parser.Extensions
{
	public sealed class LongestMatch<TResult, TUserState> : FSharpFunc<CharStream<TUserState>, Reply<TResult>>
	{
		private readonly string name;
		public string Name { get { return name; } }

		private ErrorMessageList error;

		private readonly IList<FSharpFunc<CharStream<TUserState>, Reply<TResult>>> parsers;
		public IList<FSharpFunc<CharStream<TUserState>, Reply<TResult>>> Parsers { get { return parsers; } }

		public LongestMatch(string name)
		{
			parsers = new List<FSharpFunc<CharStream<TUserState>, Reply<TResult>>>();
			this.name = name;
			error = new ErrorMessageList(new ErrorMessage.Expected(name));
		}

		public LongestMatch(string name, IEnumerable<FSharpFunc<CharStream<TUserState>, Reply<TResult>>> parsers)
			: this(name)
		{
			foreach (var parser in parsers)
			{
				Parsers.Add(parser);
			}
		}

		public FSharpFunc<CharStream<TUserState>, Reply<TResult>> AsParser { get { return this; } }

		public override Reply<TResult> Invoke(CharStream<TUserState> stream)
		{
			var parsers = Parsers;
			if (parsers.Count == 0)
			{
				return new Reply<TResult>(ReplyStatus.Error, error);
			}

			CharStreamState<TUserState> startState = stream.State;
			long startTag = startState.Tag;

			Reply<TResult> bestReply = parsers[0].Invoke(stream);
			long bestIndex = stream.Index;
			CharStreamState<TUserState> bestState = stream.State;

			for (int i = 1; i < parsers.Count; i++)
			{
				if (stream.StateTag != startTag) stream.BacktrackTo(startState);

				Reply<TResult> currentReply = parsers[i].Invoke(stream);

				// If we progressed at all?
				if (stream.StateTag != startTag)
				{
					long currentIndex = stream.Index;

					if (currentIndex > bestIndex)
					{
						// This parser has gone further, and so use this result
						bestIndex = currentIndex;
						bestReply = currentReply;
						bestState = stream.State;
					}
					else if (currentIndex == bestIndex)
					{
						if (bestReply.Status == currentReply.Status)
						{
							if (bestReply.Status == ReplyStatus.Ok)
							{
								throw new NotImplementedException("Cannot handle two identical length successes: " + bestReply.Result + " and " + currentReply.Result);
							}
							else
							{
								// Both failed with the same error code. Merge the types
								bestReply.Error = ErrorMessageList.Merge(bestReply.Error, currentReply.Error);
							}
						}
						else if (currentReply.Status == ReplyStatus.Ok)
						{
							// This parser has succeeded and the other has failed
							bestReply = currentReply;
							bestState = stream.State;
						}
						else
						{
							// Both have failed.
							bestReply.Error = ErrorMessageList.Merge(bestReply.Error, currentReply.Error);

							// The other is a normal error, so raise it to a fatal error
							if (bestReply.Status == ReplyStatus.FatalError)
							{
								bestReply.Status = ReplyStatus.FatalError;
							}
						}
					}
				}
			}
                
			if (startTag == bestState.Tag)
			{
				stream.BacktrackTo(startState);
				return new Reply<TResult>(ReplyStatus.Error, error);
			}

			stream.BacktrackTo(bestState);
			return bestReply;
		}
	}
}

