using FileHelpers;

namespace Notif.Services.Worker.Model
{
    [FixedLengthRecord(FixedMode.AllowMoreChars)]
    public class ModelResponse
    {
        [FieldOrder(1)]
        [FieldFixedLength(2)]
        public string responseCode;

        [FieldOrder(2)]
        [FieldFixedLength(200)]
        public string descErrorCode;

        [FieldOrder(3)]
        [FieldFixedLength(200)]
        public string descErrorCodeEN;

        [FieldOrder(4)]
        public object responseData;

    }
}
