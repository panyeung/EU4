namespace SengokuChronicle.Persistence
{
    /// <summary>Machine-readable failure reasons for save/load operations.</summary>
    public enum SaveErrorCode
    {
        None = 0,
        NotFound,
        ReadFailed,
        InvalidJson,
        UnsupportedVersion,
        InvalidData,
        WriteFailed
    }

    /// <summary>Result of validating or writing save data; never throws to callers.</summary>
    public readonly struct SaveOperationResult
    {
        private SaveOperationResult(bool succeeded, SaveErrorCode error, string message)
        {
            Succeeded = succeeded;
            Error = error;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public SaveErrorCode Error { get; }
        public string Message { get; }

        public static SaveOperationResult Success()
        {
            return new SaveOperationResult(true, SaveErrorCode.None, string.Empty);
        }

        public static SaveOperationResult Failure(SaveErrorCode error, string message)
        {
            return new SaveOperationResult(false, error, message);
        }
    }

    /// <summary>Result of reading a save slot; carries validated data or a specific error.</summary>
    public readonly struct SaveLoadResult
    {
        public SaveLoadResult(SaveData data, SaveErrorCode error, string message)
        {
            Data = data;
            Error = error;
            Message = message ?? string.Empty;
        }

        public SaveData Data { get; }
        public SaveErrorCode Error { get; }
        public string Message { get; }
        public bool Succeeded => Error == SaveErrorCode.None && Data != null;
    }
}
