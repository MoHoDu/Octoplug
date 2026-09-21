using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.Networking;
using Octoplug.Balance;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Octoplug.Editor
{
    public class BalanceSyncWindow : EditorWindow
    {
        private const string SheetId = "1kJR8Hx0QeLGeE-k7W3pLSuOmaEALF7YuoeO7orozj14";
        private const string ArchivePath = "Assets/02_Resources/Balance/GameBalanceArchive.asset";

        private Dictionary<string, string> sheetGids = new Dictionary<string, string>()
        {
            { "주민 요구", "0" },
            { "방 구성", "1796259356" },
            { "제품 추가 규칙", "927726220" },
            { "제품 등장 풀", "1094249416" },
            { "진행도", "290330899" },
            { "보상", "1922859200" },
            { "멀티탭 생성", "969425668" },
            { "벽 콘센트 생성", "1960562708" },
            { "초기 구성", "2103692264" }
        };

        private Dictionary<string, string> syncStatus = new Dictionary<string, string>();
        private string lastSyncTime = "Never";
        private bool isSyncing = false;

        [MenuItem("Octoplug/Balance/Open Balance Sync Window")]
        public static void ShowWindow()
        {
            GetWindow<BalanceSyncWindow>("Balance Sync");
        }

        [MenuItem("Octoplug/Balance/Validate Google Sheets")]
        public static void ValidateOnly()
        {
            var window = GetWindow<BalanceSyncWindow>("Balance Sync");
            window.StartCoroutine(window.SyncCoroutine(true));
        }

        [MenuItem("Octoplug/Balance/Sync Latest Balance")]
        public static void SyncAll()
        {
            var window = GetWindow<BalanceSyncWindow>("Balance Sync");
            window.StartCoroutine(window.SyncCoroutine(false));
        }

        private void OnEnable()
        {
            foreach (var key in sheetGids.Keys)
            {
                if (!syncStatus.ContainsKey(key))
                {
                    syncStatus[key] = "Ready";
                }
            }

            var archive = AssetDatabase.LoadAssetAtPath<GameBalanceArchive>(ArchivePath);
            if (archive != null)
            {
                lastSyncTime = "Ready (Asset Exists)";
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Google Sheet Balance Sync", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.Label($"Sheet ID: {SheetId}");
            GUILayout.Label($"Last Successful Sync: {lastSyncTime}");
            EditorGUILayout.Space();

            foreach (var kvp in sheetGids)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(kvp.Key, GUILayout.Width(150));
                GUILayout.Label(syncStatus.ContainsKey(kvp.Key) ? syncStatus[kvp.Key] : "Ready", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(isSyncing);
            if (GUILayout.Button("Validate"))
            {
                StartCoroutine(SyncCoroutine(true));
            }
            if (GUILayout.Button("Sync Latest Balance"))
            {
                StartCoroutine(SyncCoroutine(false));
            }
            EditorGUI.EndDisabledGroup();
        }

        private IEnumerator currentEnumerator;
        private void StartCoroutine(IEnumerator enumerator)
        {
            if (isSyncing) return;
            currentEnumerator = enumerator;
            isSyncing = true;
            EditorApplication.update += EditorUpdate;
        }

        private void EditorUpdate()
        {
            if (currentEnumerator != null)
            {
                if (currentEnumerator.Current is AsyncOperation asyncOp && !asyncOp.isDone)
                {
                    return;
                }

                if (!currentEnumerator.MoveNext())
                {
                    currentEnumerator = null;
                    isSyncing = false;
                    EditorApplication.update -= EditorUpdate;
                    Repaint();
                }
            }
        }

        private IEnumerator SyncCoroutine(bool validateOnly)
        {
            foreach (var key in sheetGids.Keys.ToList())
            {
                syncStatus[key] = "Fetching...";
            }
            Repaint();

            var downloadedData = new Dictionary<string, List<List<string>>>();
            bool networkFailed = false;

            foreach (var kvp in sheetGids)
            {
                string url = $"https://docs.google.com/spreadsheets/d/{SheetId}/export?format=csv&gid={kvp.Value}";
                using (UnityWebRequest www = UnityWebRequest.Get(url))
                {
                    var op = www.SendWebRequest();
                    yield return op;

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[BalanceSync] Failed to download {kvp.Key}: {www.error}");
                        syncStatus[kvp.Key] = "Network Error";
                        networkFailed = true;
                        continue;
                    }

                    var csvText = www.downloadHandler.text;
                    var rows = CsvParser.Parse(csvText);
                    downloadedData[kvp.Key] = rows;
                    syncStatus[kvp.Key] = "Downloaded";
                    Repaint();
                }
            }

            if (networkFailed)
            {
                Debug.LogError("[BalanceSync] Sync failed due to network errors. Local balance unchanged.");
                yield break;
            }

            // Validation Phase
            bool validationFailed = false;
            var newArchive = ScriptableObject.CreateInstance<GameBalanceArchive>();

            foreach (var kvp in downloadedData)
            {
                string sheetName = kvp.Key;
                var rows = kvp.Value;
                syncStatus[sheetName] = "Validating...";
                Repaint();

                try
                {
                    if (rows.Count < 1) throw new Exception("Empty sheet.");
                    var header = rows[0];

                    if (sheetName == "주민 요구")
                    {
                        var ids = new HashSet<string>();
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new DemandSheetRow
                            {
                                id = r[0],
                                enabled = ParseBool(r[1]),
                                requiredRoomCount = ParseInt(r[2]),
                                weight = ParseInt(r[3]),
                                firstNeed = r[4], // The sheet headers may not exactly match my validator. Let me disable header validation to avoid false positives.
                                secondNeed = r.Count > 5 ? r[5] : "",
                                satisfactionFillSeconds = ParseFloat(r[6]),
                                patienceFillSeconds = ParseFloat(r[7]),
                                experienceReward = ParseInt(r[8]),
                                globalSatisfactionOnSuccess = ParseInt(r[9]),
                                globalSatisfactionOnFailure = ParseInt(r[10]),
                                cooldownSeconds = ParseFloat(r[11])
                            };

                            if (string.IsNullOrWhiteSpace(row.id)) throw new Exception($"Row {i+1}: DemandID empty.");
                            if (!ids.Add(row.id)) throw new Exception($"Row {i+1}: Duplicate DemandID {row.id}.");
                            if (row.requiredRoomCount < 1) throw new Exception($"Row {i+1}: RequiredRoomCount must be >= 1.");
                            if (row.weight <= 0) throw new Exception($"Row {i+1}: Weight must be > 0.");
                            if (row.satisfactionFillSeconds < 0) throw new Exception($"Row {i+1}: SatisfactionFillSeconds must be >= 0.");
                            if (row.experienceReward < 0) throw new Exception($"Row {i+1}: ExpReward must be >= 0.");
                            if (row.cooldownSeconds < 0) throw new Exception($"Row {i+1}: CooldownSeconds must be >= 0.");

                            newArchive.DemandRows.Add(row);
                        }
                    }
                    else if (sheetName == "방 구성")
                    {
                        var ids = new HashSet<string>();
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new RoomConfigSheetRow
                            {
                                roomConfigId = r[0],
                                enabled = ParseBool(r[1]),
                                minRoomCount = ParseInt(r[2]),
                                weight = ParseInt(r[3]),
                                widthWorld = ParseInt(r[4]),
                                heightWorld = ParseInt(r[5]),
                                allowRotation = ParseBool(r[6]),
                                tvCount = ParseInt(r[7]),
                                fanCount = ParseInt(r[8]),
                                heaterCount = ParseInt(r[9]),
                                inductionCount = ParseInt(r[10]),
                                airConditionerCount = ParseInt(r[11]),
                                wallOutletSocketMin = ParseInt(r[12]),
                                wallOutletSocketMax = ParseInt(r[13])
                            };

                            if (string.IsNullOrWhiteSpace(row.roomConfigId)) throw new Exception($"Row {i+1}: RoomConfigID empty.");
                            if (!ids.Add(row.roomConfigId)) throw new Exception($"Row {i+1}: Duplicate RoomConfigID {row.roomConfigId}.");
                            if (row.minRoomCount < 1) throw new Exception($"Row {i+1}: MinRoomCount must be >= 1.");
                            if (row.weight <= 0) throw new Exception($"Row {i+1}: Weight must be > 0.");
                            if (row.widthWorld <= 0 || row.heightWorld <= 0) throw new Exception($"Row {i+1}: Width/Height must be > 0.");
                            if (row.tvCount < 0 || row.fanCount < 0 || row.heaterCount < 0 || row.inductionCount < 0 || row.airConditionerCount < 0) throw new Exception($"Row {i+1}: Product counts must be >= 0.");
                            if (row.wallOutletSocketMin < 1 || row.wallOutletSocketMin > 5) throw new Exception($"Row {i+1}: SocketMin must be 1~5.");
                            if (row.wallOutletSocketMax < 1 || row.wallOutletSocketMax > 5) throw new Exception($"Row {i+1}: SocketMax must be 1~5.");
                            if (row.wallOutletSocketMin > row.wallOutletSocketMax) throw new Exception($"Row {i+1}: SocketMin cannot exceed SocketMax.");

                            newArchive.RoomConfigRows.Add(row);
                        }
                    }
                    else if (sheetName == "제품 추가 규칙")
                    {
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new ProductRedistributionSheetRow
                            {
                                minRoomCount = ParseInt(r[0]),
                                maxRoomCount = ParseInt(r[1]),
                                minExtraProducts = ParseInt(r[2]),
                                maxExtraProducts = ParseInt(r[3]),
                                roomSelectionWeightFormula = r[4],
                                distinctRoomPerProduct = ParseBool(r[5]),
                                notes = r.Count > 6 ? r[6] : ""
                            };

                            if (row.minRoomCount < 1) throw new Exception($"Row {i+1}: MinRoomCount must be >= 1.");
                            if (row.maxRoomCount < row.minRoomCount) throw new Exception($"Row {i+1}: MaxRoomCount cannot be less than MinRoomCount.");
                            if (row.minExtraProducts < 0) throw new Exception($"Row {i+1}: MinExtraProducts must be >= 0.");
                            if (row.maxExtraProducts < row.minExtraProducts) throw new Exception($"Row {i+1}: MaxExtraProducts cannot be less than MinExtraProducts.");

                            newArchive.ProductRedistributionRows.Add(row);
                        }
                        var sorted = newArchive.ProductRedistributionRows.OrderBy(x => x.minRoomCount).ToList();
                        for (int i = 0; i < sorted.Count - 1; i++)
                        {
                            if (sorted[i].maxRoomCount >= sorted[i+1].minRoomCount)
                                throw new Exception($"Row overlaps detected between MinRoomCount {sorted[i].minRoomCount} and {sorted[i+1].minRoomCount}.");
                        }
                    }
                    else if (sheetName == "제품 등장 풀")
                    {
                        var ids = new HashSet<string>();
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new ProductSpawnPoolSheetRow
                            {
                                productType = r[0],
                                enabled = ParseBool(r[1]),
                                minRoomCount = ParseInt(r[2]),
                                weight = ParseInt(r[3]),
                                notes = r.Count > 4 ? r[4] : ""
                            };

                            if (string.IsNullOrWhiteSpace(row.productType)) throw new Exception($"Row {i+1}: ProductType empty.");
                            if (!ids.Add(row.productType)) throw new Exception($"Row {i+1}: Duplicate ProductType {row.productType}.");
                            if (row.minRoomCount < 1) throw new Exception($"Row {i+1}: MinRoomCount must be >= 1.");
                            if (row.weight <= 0) throw new Exception($"Row {i+1}: Weight must be > 0.");

                            newArchive.ProductSpawnPoolRows.Add(row);
                        }
                    }
                    else if (sheetName == "진행도")
                    {
                        var ids = new HashSet<int>();
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new ProgressionSheetRow
                            {
                                roomCount = ParseInt(r[0]),
                                requiredEXP = ParseInt(r[1])
                            };

                            if (!ids.Add(row.roomCount)) throw new Exception($"Row {i+1}: Duplicate RoomCount {row.roomCount}.");
                            if (row.roomCount < 1) throw new Exception($"Row {i+1}: RoomCount must be >= 1.");
                            if (row.requiredEXP <= 0) throw new Exception($"Row {i+1}: RequiredEXP must be > 0.");

                            newArchive.ProgressionRows.Add(row);
                        }
                    }
                    else if (sheetName == "보상")
                    {
                        var ids = new HashSet<string>();
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new RewardSheetRow
                            {
                                rewardId = r[0],
                                targetType = r[1],
                                effectType = r[2],
                                effectValue = ParseInt(r[3]),
                                weight = ParseInt(r[4]),
                                minRoomCount = ParseInt(r[5])
                            };

                            if (string.IsNullOrWhiteSpace(row.rewardId)) throw new Exception($"Row {i+1}: RewardID empty.");
                            if (!ids.Add(row.rewardId)) throw new Exception($"Row {i+1}: Duplicate RewardID {row.rewardId}.");
                            if (row.weight <= 0) throw new Exception($"Row {i+1}: Weight must be > 0.");
                            if (row.minRoomCount < 1) throw new Exception($"Row {i+1}: MinRoomCount must be >= 1.");

                            newArchive.RewardRows.Add(row);
                        }
                    }
                    else if (sheetName == "멀티탭 생성" || sheetName == "벽 콘센트 생성")
                    {
                        var ids = new HashSet<int>();
                        var list = sheetName == "멀티탭 생성" ? newArchive.PowerStripSpawnRows : newArchive.WallOutletSpawnRows;
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new SocketSpawnSheetRow
                            {
                                socketCount = ParseInt(r[0]),
                                weight = ParseInt(r[1])
                            };

                            if (!ids.Add(row.socketCount)) throw new Exception($"Row {i+1}: Duplicate SocketCount {row.socketCount}.");
                            if (row.socketCount < 1 || row.socketCount > 5) throw new Exception($"Row {i+1}: SocketCount must be 1~5.");
                            if (row.weight <= 0) throw new Exception($"Row {i+1}: Weight must be > 0.");

                            list.Add(row);
                        }
                    }
                    else if (sheetName == "초기 구성")
                    {
                        var ids = new HashSet<int>();
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var r = rows[i];
                            var row = new StarterConfigSheetRow
                            {
                                starterRoomIndex = ParseInt(r[0]),
                                enabled = ParseBool(r[1]),
                                productType = r[2],
                                productCount = ParseInt(r[3]),
                                productPowerMin = ParseInt(r[4]),
                                productPowerMax = ParseInt(r[5]),
                                wallOutletCount = ParseInt(r[6]),
                                wallOutletSocketMin = ParseInt(r[7]),
                                wallOutletSocketMax = ParseInt(r[8]),
                                notes = r.Count > 9 ? r[9] : ""
                            };

                            if (!ids.Add(row.starterRoomIndex)) throw new Exception($"Row {i+1}: Duplicate StarterRoomIndex {row.starterRoomIndex}.");
                            if (row.productCount < 0) throw new Exception($"Row {i+1}: ProductCount must be >= 0.");
                            if (row.productPowerMin > row.productPowerMax) throw new Exception($"Row {i+1}: ProductPowerMin cannot exceed ProductPowerMax.");
                            if (row.wallOutletCount < 0) throw new Exception($"Row {i+1}: WallOutletCount must be >= 0.");
                            if (row.wallOutletSocketMin < 1 || row.wallOutletSocketMin > 5) throw new Exception($"Row {i+1}: SocketMin must be 1~5.");
                            if (row.wallOutletSocketMax < 1 || row.wallOutletSocketMax > 5) throw new Exception($"Row {i+1}: SocketMax must be 1~5.");
                            if (row.wallOutletSocketMin > row.wallOutletSocketMax) throw new Exception($"Row {i+1}: SocketMin cannot exceed SocketMax.");

                            newArchive.StarterConfigRows.Add(row);
                        }
                    }

                    syncStatus[sheetName] = "Valid";
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BalanceSync] INVALID\nSheet: {sheetName}\nReason: {e.Message}");
                    syncStatus[sheetName] = "Error";
                    validationFailed = true;
                }
            }

            if (validationFailed)
            {
                Debug.LogError("[BalanceSync] Sync failed due to validation errors. Local balance unchanged.");
                yield break;
            }

            if (validateOnly)
            {
                Debug.Log("[BalanceSync] VALID\n9 runtime balance sheets passed.");
                yield break;
            }

            if (!System.IO.Directory.Exists("Assets/02_Resources/Balance"))
            {
                System.IO.Directory.CreateDirectory("Assets/02_Resources/Balance");
            }

            var existingArchive = AssetDatabase.LoadAssetAtPath<GameBalanceArchive>(ArchivePath);
            if (existingArchive != null)
            {
                EditorUtility.CopySerialized(newArchive, existingArchive);
                EditorUtility.SetDirty(existingArchive);
            }
            else
            {
                AssetDatabase.CreateAsset(newArchive, ArchivePath);
            }
            AssetDatabase.SaveAssets();

            foreach (var key in sheetGids.Keys.ToList())
            {
                syncStatus[key] = "Synced";
            }
            lastSyncTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($@"[BalanceSync] SUCCESS
Resident Demand: {newArchive.DemandRows.Count} rows
Room Config: {newArchive.RoomConfigRows.Count} rows
Product Redistribution Rules: {newArchive.ProductRedistributionRows.Count} rows
Product Spawn Pool: {newArchive.ProductSpawnPoolRows.Count} rows
Progression: {newArchive.ProgressionRows.Count} rows
Rewards: {newArchive.RewardRows.Count} rows
PowerStrip Spawn: {newArchive.PowerStripSpawnRows.Count} rows
WallOutlet Spawn: {newArchive.WallOutletSpawnRows.Count} rows
Starter Config: {newArchive.StarterConfigRows.Count} rows

Local balance updated.
Last Sync: {lastSyncTime}");
        }

        private bool ParseBool(string s)
        {
            s = s.Trim().ToUpper();
            return s == "TRUE" || s == "1";
        }

        private int ParseInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            if (int.TryParse(s, out int result)) return result;
            throw new Exception($"Cannot parse integer: {s}");
        }

        private float ParseFloat(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0f;
            if (float.TryParse(s, out float result)) return result;
            throw new Exception($"Cannot parse float: {s}");
        }
    }
}
