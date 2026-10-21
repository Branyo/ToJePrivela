namespace ToJePrivela.Application.Common;

/// <summary>A request that calls the AI only in some cases, so a limit on AI use can let the other cases through.</summary>
public interface IMayUseAi
{
    /// <summary>Whether handling the request may call the AI (generation or translation).</summary>
    bool UsesAi();
}
