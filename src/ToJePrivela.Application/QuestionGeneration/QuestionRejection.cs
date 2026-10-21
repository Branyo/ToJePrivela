namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>Why a generated or imported question is not stored.</summary>
public enum QuestionRejection
{
    /// <summary>Not an object with the texts and the answer (<c>questionSk</c>, <c>questionEn</c>, <c>answer</c>).</summary>
    Malformed,

    /// <summary>A text the domain rejects (too short or too long).</summary>
    InvalidText,

    /// <summary>Not a plain number, such as <c>42</c> or <c>-3.5</c>.</summary>
    InvalidAnswer,

    /// <summary>Above <see cref="GeneratedQuestionFilter.MaxAnswer"/>.</summary>
    AnswerTooLarge,

    /// <summary>A text gives the answer away.</summary>
    StatesAnswer,

    /// <summary>The category already has the question.</summary>
    Duplicate,

    /// <summary>An earlier item of the same import has the question.</summary>
    Repeated
}
