namespace UnitTests.Helpers
{
    public static class PagingResultExtensions
    {
        public static int TotalCount(this object result)
            => (int)(result.GetType().GetProperty("TotalCount")!.GetValue(result) ?? 0);

        public static IEnumerable<object> Items(this object result)
            => (IEnumerable<object>)(result.GetType().GetProperty("Items")!.GetValue(result)
               ?? Array.Empty<object>());
    }
}
