using System;

namespace HPKata.Core.Excel
{
    /// <summary>
    /// Ambient provider for IKataExcelAdapter, allowing dependency injection or fallback to default adapter.
    /// </summary>
    public static class KataExcelAdapterProvider
    {
        private static IKataExcelAdapter? _instance;

        public static IKataExcelAdapter Current => _instance ??= new KataExcelAdapter();

        public static void SetInstance(IKataExcelAdapter? adapter)
        {
            _instance = adapter;
        }

        public static void Reset()
        {
            _instance = null;
        }
    }
}
