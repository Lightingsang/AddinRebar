using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Core.Common;
using Microsoft.Data.Sqlite;

namespace HPDcePro.Core.MachineLearning
{
    public class MauHocSample
    {
        public long Id { get; set; }
        public string TextGoc { get; set; } = string.Empty;
        public int ChiSoSo { get; set; }
        public int GiaTri { get; set; }
        public string Feature { get; set; } = string.Empty;
        public string Nhan { get; set; } = string.Empty;
        public string Nguon { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }

    public class LichSuSuaRecord
    {
        public long Id { get; set; }
        public string TextGoc { get; set; } = string.Empty;
        public string Truoc { get; set; } = string.Empty;
        public string Sau { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
    }

    public class CauHinhModelRecord
    {
        public string Ten { get; set; } = string.Empty;
        public string DuongDan { get; set; } = string.Empty;
        public int SoMau { get; set; }
        public string ThoiGian { get; set; } = string.Empty;
    }

    /// <summary>
    /// SQLite data access service for mauhoc.db via AssetPathResolver.
    /// Reads training samples, user correction history, and model configuration metadata.
    /// </summary>
    public class MauHocDatabaseReader : IDisposable
    {
        public static string DefaultDatabasePath => AssetPathResolver.ResolveMachineLearningDatabase();
        private readonly string _connectionString;
        private bool _disposed;

        public MauHocDatabaseReader(string? dbPath = null)
        {
            string path = string.IsNullOrWhiteSpace(dbPath) ? DefaultDatabasePath : dbPath!;
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly
            };
            _connectionString = builder.ConnectionString;
        }

        public bool IsDisposed => _disposed;

        private SqliteConnection CreateConnection()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MauHocDatabaseReader));
            }
            return new SqliteConnection(_connectionString);
        }

        public async Task<int> GetSampleCountAsync(CancellationToken cancellationToken = default)
        {
            using (var conn = CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM MauHoc";
                    object? result = await cmd.ExecuteScalarAsync(cancellationToken);
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<List<MauHocSample>> GetSamplesAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
        {
            var list = new List<MauHocSample>();
            using (var conn = CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, TextGoc, ChiSoSo, GiaTri, Feature, Nhan, Nguon, ThoiGian, Hash FROM MauHoc ORDER BY Id LIMIT @limit OFFSET @offset";
                    cmd.Parameters.AddWithValue("@limit", limit);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            list.Add(ReadSample(reader));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<string>> GetDistinctLabelsAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<string>();
            using (var conn = CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT Nhan FROM MauHoc WHERE Nhan IS NOT NULL AND Nhan != '' ORDER BY Nhan";
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            list.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<LichSuSuaRecord>> GetLichSuSuaAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<LichSuSuaRecord>();
            using (var conn = CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, TextGoc, Truoc, Sau, ThoiGian FROM LichSuSua ORDER BY Id";
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            list.Add(new LichSuSuaRecord
                            {
                                Id = reader.GetInt64(0),
                                TextGoc = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Truoc = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Sau = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                ThoiGian = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<CauHinhModelRecord>> GetCauHinhModelsAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<CauHinhModelRecord>();
            using (var conn = CreateConnection())
            {
                await conn.OpenAsync(cancellationToken);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Ten, DuongDan, SoMau, ThoiGian FROM CauHinhModel";
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            list.Add(new CauHinhModelRecord
                            {
                                Ten = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                DuongDan = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                SoMau = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                ThoiGian = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            return list;
        }

        private static MauHocSample ReadSample(SqliteDataReader reader)
        {
            return new MauHocSample
            {
                Id = reader.GetInt64(0),
                TextGoc = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ChiSoSo = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                GiaTri = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                Feature = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Nhan = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Nguon = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                ThoiGian = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Hash = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
            };
        }

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
