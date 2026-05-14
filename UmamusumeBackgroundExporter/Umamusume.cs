using Microsoft.Data.Sqlite;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uma
{
    public class Config
    {

    }

    public class UmaDatabase
    {
        public static string persistentPath;// = "E:\\Uma\\Persistent\\";
        public static string masterDbPath = "E:\\Uma\\Persistent\\master\\master.mdb";
        public static string metaDbPath = "E:\\Uma\\Persistent\\meta";
        public static string DBKey = "6D5B65336336632554712D73505363386D34377B356370233734532973433633";
        public static string DBBaseKey = "F170CEA4DFCEA3E1A5D8C70BD1000000";
        public static string ABKey = "532B4631E4A7B9473E7CFB";

        public static string PersistentPath
        {
            get { return persistentPath; }
            set
            {
                persistentPath = value;
                masterDbPath = Path.Join(value, "master\\master.mdb");
                metaDbPath = Path.Join(value, "meta");
            }
        }

        public static SqliteConnection mdbConn;

        public static List<CharaData> CharaData = new List<CharaData>();
        //public static List<DataRow> MobCharaData;
        //public static List<FaceTypeData> FaceTypeData;
        public static List<DressData> DressData;
        //public static List<DataRow> CharaNameData;
        public static List<CharaMotionSet> CharaMotionSet = new();

        public static Dictionary<string, UmaDatabaseEntry> MetaData;

        public static string BodyPath = "3d/chara/body/";
        public static string MiniBodyPath = "3d/chara/mini/body/";
        public static string HeadPath = "3d/chara/head/";
        public static string TailPath = "3d/chara/tail/";
        public static string MotionPath = "3d/motion/";
        public static string CharaPath = "3d/chara/";
        public static string EffectPath = "3d/effect/";
        public static string CostumePath = "outgame/dress/";

        public static void CreateConnection()
        {
            try
            {
                mdbConn = new SqliteConnection($"Data Source={masterDbPath};");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error creating database connections: " + e);
                throw;
            }
        }

        public static void Initialize()
        {
            try
            {
                mdbConn.Open();
                //ReadCharaData(mdbConn);//CharaData = ReadMaster(mdbConn, "SELECT * FROM chara_data C,(SELECT D.'index' charaid,D.'text' charaname FROM text_data D WHERE id like 6) T WHERE C.id like T.charaid            

                //MobCharaData = ReadMaster(mdbConn, "SELECT * FROM mob_data M,(SELECT D.'index' charaid,D.'text' charaname FROM text_data D WHERE id like 59) T WHERE M.mob_id like T.charaid");
                //FaceTypeData = ReadFaceTypeData(mdbConn);
                //DressData = ReadMaster(mdbConn, "SELECT * FROM dress_data C,(SELECT D.'index' dressid,D.'text' dressname FROM text_data D WHERE id like 14) T WHERE C.id like T.dressid");
                //CharaNameData = ReadMaster(mdbConn, "SELECT * FROM text_data WHERE id = 372");
                //ReadCharaMotionSet(mdbConn);//CharaMotionSet = ReadMaster(mdbConn, "SELECT * FROM chara_motion_set WHERE id > 1001000 AND id < 9100125;");
                //DressData = ReadDressData(mdbConn);

                MetaData = ReadMetaFromEncryptedDb(metaDbPath, GenFinalKey(Utility.HexStringToBytes(DBKey)), 3);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading data: " + e);
                throw;
            }

            static Dictionary<string, UmaDatabaseEntry> ReadMetaFromEncryptedDb(string dbPath, byte[] keyBytes, int cipherIndex = -1)
            {
                var meta = new Dictionary<string, UmaDatabaseEntry>(StringComparer.Ordinal);
                IntPtr db = IntPtr.Zero;

                try
                {
                    db = Sqlite3MC.Open(dbPath);

                    if (cipherIndex >= 0)
                    {
                        try
                        {
                            int cfgRc = Sqlite3MC.MC_Config(db, "cipher", cipherIndex);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"MC_Config thrown: {e}");
                        }
                    }

                    int rcKey = Sqlite3MC.Key_SetBytes(db, keyBytes);
                    if (rcKey != Sqlite3MC.SQLITE_OK)
                    {
                        string em = Sqlite3MC.GetErrMsg(db);
                        throw new InvalidOperationException($"sqlite3_key returned rc={rcKey}, errmsg={em}");
                    }

                    if (!Sqlite3MC.ValidateReadable(db, out string validateErr))
                    {
                        Console.WriteLine($"DB validation after key failed: {validateErr}");
                        throw new InvalidOperationException("DB validation after key failed: " + validateErr);
                    }



                    string sql = "SELECT m,n,h,c,d,e FROM a";
                    Sqlite3MC.ForEachRow(sql, db, (stmt) =>
                    {
                        try
                        {
                            string m = Sqlite3MC.ColumnText(stmt, 0);
                            string n = Sqlite3MC.ColumnText(stmt, 1);
                            string h = Sqlite3MC.ColumnText(stmt, 2);
                            string c = Sqlite3MC.ColumnText(stmt, 3);
                            string d = Sqlite3MC.ColumnText(stmt, 4);
                            long e = Sqlite3MC.ColumnInt64(stmt, 5);

                            if (string.IsNullOrEmpty(m))
                            {
                                Console.WriteLine("Skipping row: empty type string (m).");
                                return;
                            }

                            if (!Enum.TryParse(m, /*ignoreCase*/ false, out UmaFileType type))
                            {
                                Console.WriteLine($"Unrecognized EntryType Enum Value :{m}");
                                return;
                            }

                            if (string.IsNullOrEmpty(n))
                            {
                                Console.WriteLine($"Invalid entry name '{n}' or URL '{h}'. Skipping row.");
                                return;
                            }

                            var entry = new UmaDatabaseEntry
                            {
                                Type = type,
                                Name = n,
                                Url = h,
                                Checksum = c,
                                Prerequisites = d,
                                Key = e
                            };

                            if (!meta.ContainsKey(entry.Name))
                            {
                                meta.Add(entry.Name, entry);
                            }
                        }
                        catch (Exception exRow)
                        {
                            Console.WriteLine("Error caught while reading row: " + exRow);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ReadMetaFromEncryptedDb failed: " + ex);
                    throw;
                }
                finally
                {
                    if (db != IntPtr.Zero)
                    {
                        try { Sqlite3MC.Close(db); }
                        catch (Exception e) { Console.WriteLine("Closing DB failed: " + e); }
                    }
                }

                return meta;
            }
            static byte[] GenFinalKey(byte[] key)
            {
                byte[] baseKey = Utility.HexStringToBytes(DBBaseKey);
                if (baseKey.Length < 13)
                    throw new Exception("Invalid Base Key length");
                for (int i = 0; i < key.Length; i++)
                {
                    key[i] = (byte)(key[i] ^ baseKey[i % 13]);
                }
                return key;
            }
        }
        public static string? ResolvePath(string logicalPath)
        {
            //return MetaData[logicalPath]?.QueryPath();

            if (MetaData.TryGetValue(logicalPath, out var path))
            {
                return path.QueryPath();
            }
            else
            {
                return null;
            }
        }
    }

    public class UmaAssetBundleStream : FileStream
    {
        private const int headerSize = 256;
        private readonly byte[] baseKeys = Utility.HexStringToBytes(UmaDatabase.ABKey);
        private readonly byte[] keys;

        public UmaAssetBundleStream(string filename, byte[] keys) : base(filename, FileMode.Open, FileAccess.Read)
        {
            this.keys = keys;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long StartPosition = Position;
            int res = base.Read(buffer, offset, count);

            for (long i = (StartPosition < headerSize ? headerSize - StartPosition : 0); i < count; i++)
            {
                buffer[i] ^= keys[(StartPosition + i) % (baseKeys.Length * 8)];
            }

            return res;
        }
    }



    /*public class UmaAssetBundleStream : FileStream
    {
        private const int headerSize = 256;
        private readonly byte[] baseKeys = Utility.HexStringToBytes(UmaDatabase.ABKey);
        private readonly byte[] keys;

        public UmaAssetBundleStream(string filename, byte[] keys)
            : base(filename, FileMode.Open, FileAccess.Read)
        {
            this.keys = keys;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long startPosition = Position;
            int bytesRead = base.Read(buffer, offset, count);

            if (bytesRead <= 0)
                return bytesRead;

            long decryptStart = startPosition < headerSize
                ? headerSize - startPosition
                : 0;

            for (long i = decryptStart; i < bytesRead; i++)
            {
                buffer[offset + i] ^= keys[(startPosition + i) % keys.Length];
            }

            return bytesRead;
        }
   
    }*/
}
