using UnityEngine;
using Unity.Netcode;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TicTacToeLocal : NetworkBehaviour
{
    [Header("=== Prefabs ===")]
    public GameObject xPrefab;
    public GameObject oPrefab;

    [Header("=== Bảng game ===")]
    public float cellSize   = 1.0f;
    public float boardDepth = 0.1f;
    public Color boardColor = new Color(0.2f, 0.2f, 0.25f);
    public Color lineColor  = new Color(0.8f, 0.8f, 0.8f);
    public Color winColor   = new Color(1.0f, 0.85f, 0.0f);

    // ── Network State ──────────────────────────────────────────────────
    // board[i]: 0 = trống, 1 = X, 2 = O
    private NetworkVariable<int> _turn     = new NetworkVariable<int>(1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _gameOver = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 9 NetworkVariables riêng biệt vì mảng NetworkVariable không được Netcode tự động nhận diện
    private NetworkVariable<int> _cell0 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell1 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell2 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell3 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell4 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell5 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell6 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell7 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _cell8 = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> GetCell(int index)
    {
        switch (index)
        {
            case 0: return _cell0;
            case 1: return _cell1;
            case 2: return _cell2;
            case 3: return _cell3;
            case 4: return _cell4;
            case 5: return _cell5;
            case 6: return _cell6;
            case 7: return _cell7;
            case 8: return _cell8;
            default: return _cell0;
        }
    }

    // ── Local refs ────────────────────────────────────────────────────
    private GameObject[] cellColliders = new GameObject[9];
    private GameObject[] pieces        = new GameObject[9];
    private GameObject   resetButton;
    private bool         boardBuilt    = false;

    private static readonly int[,] winLines = {
        {0,1,2},{3,4,5},{6,7,8},
        {0,3,6},{1,4,7},{2,5,8},
        {0,4,8},{2,4,6}
    };

    // ══════════════════════════════════════════════════════════════════
    // KHỞI TẠO NETWORK VARIABLES
    // ══════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (Application.isPlaying)
        {
            boardBuilt = true;
            FindAndSetupCells();
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // NGO SPAWN
    // ══════════════════════════════════════════════════════════════════
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Khởi tạo visual state ban đầu cho client (kể cả client join muộn)
        if (resetButton != null)
        {
            resetButton.SetActive(_gameOver.Value);
        }

        // Subscribe onChange để update UI khi nhận sync từ server
        for (int i = 0; i < 9; i++)
        {
            int idx = i;
            GetCell(idx).OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal != 0)
                    SpawnPieceLocal(idx, newVal);
            };
            
            // Hiện các quân cờ đã có sẵn trên bàn
            if (GetCell(idx).Value != 0)
            {
                SpawnPieceLocal(idx, GetCell(idx).Value);
            }
        }

        _gameOver.OnValueChanged += (oldVal, newVal) =>
        {
            if (resetButton != null)
                resetButton.SetActive(newVal);
        };

        Debug.Log($"[TicTacToe] OnNetworkSpawn — IsServer:{IsServer} IsClient:{IsClient}");
    }

    // ══════════════════════════════════════════════════════════════════
    // EDIT MODE BUILD
    // ══════════════════════════════════════════════════════════════════
    void OnEnable()
    {
        if (!Application.isPlaying && !boardBuilt)
            BuildBoard();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                // Avoid modifying Prefab assets directly to prevent data corruption warnings
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
                
                ClearChildren();
                boardBuilt = false;
                BuildBoard();
            };
        }
#endif
    }

    // ══════════════════════════════════════════════════════════════════
    // FIND CELLS (Play Mode)
    // ══════════════════════════════════════════════════════════════════
    void FindAndSetupCells()
    {
        for (int i = 0; i < 9; i++)
        {
            Transform cell = transform.Find($"Cell_{i}");
            if (cell == null) { Debug.LogWarning($"[TicTacToe] Không tìm thấy Cell_{i}!"); continue; }

            cellColliders[i] = cell.gameObject;

            NetCellHandler old = cell.GetComponent<NetCellHandler>();
            if (old != null) Destroy(old);

            NetCellHandler handler = cell.gameObject.AddComponent<NetCellHandler>();
            handler.cellIndex = i;
            handler.game      = this;
        }

        Transform reset = transform.Find("ResetButton");
        if (reset != null)
        {
            resetButton = reset.gameObject;
            resetButton.SetActive(false);

            NetResetHandler oldR = reset.GetComponent<NetResetHandler>();
            if (oldR != null) Destroy(oldR);

            NetResetHandler handler = resetButton.AddComponent<NetResetHandler>();
            handler.game = this;
        }

        Debug.Log("[TicTacToe] Setup cells xong!");
    }

    // ══════════════════════════════════════════════════════════════════
    // CLICK — Client gửi lên Server
    // ══════════════════════════════════════════════════════════════════
    public void OnCellClicked(int cellIndex)
    {
        if (!Application.isPlaying) return;
        if (_gameOver.Value) return;
        if (GetCell(cellIndex).Value != 0) return;

        // Gửi ServerRpc
        PlaceMarkServerRpc(cellIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void PlaceMarkServerRpc(int cellIndex, RpcParams rpcParams = default)
    {
        // Kiểm tra lại phía server
        if (_gameOver.Value) return;
        if (GetCell(cellIndex).Value != 0) return;

        int mark = _turn.Value;
        GetCell(cellIndex).Value = mark;

        // Spawn piece cho tất cả client
        SpawnPieceClientRpc(cellIndex, mark);

        // Kiểm tra thắng
        int winner = CheckWinner();
        if (winner != 0)
        {
            _gameOver.Value = true;
            HighlightWinLineClientRpc(winner);
            Debug.Log($"[TicTacToe] {(winner == 1 ? "X" : "O")} thắng!");
            return;
        }

        if (IsBoardFull())
        {
            _gameOver.Value = true;
            Debug.Log("[TicTacToe] Hòa!");
            return;
        }

        // Đổi turn
        _turn.Value = mark == 1 ? 2 : 1;
    }

    // ══════════════════════════════════════════════════════════════════
    // RESET — Client gửi lên Server
    // ══════════════════════════════════════════════════════════════════
    public void OnResetClicked()
    {
        ResetGameServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ResetGameServerRpc()
    {
        for (int i = 0; i < 9; i++)
            GetCell(i).Value = 0;

        _turn.Value     = 1;
        _gameOver.Value = false;

        ResetVisualClientRpc();
        Debug.Log("[TicTacToe] Game reset!");
    }

    // ══════════════════════════════════════════════════════════════════
    // CLIENT RPCs — update visual cho tất cả client
    // ══════════════════════════════════════════════════════════════════
    [ClientRpc]
    void SpawnPieceClientRpc(int cellIndex, int mark)
    {
        SpawnPieceLocal(cellIndex, mark);
    }

    [ClientRpc]
    void HighlightWinLineClientRpc(int winner)
    {
        for (int l = 0; l < 8; l++)
        {
            int a = winLines[l, 0];
            int b = winLines[l, 1];
            int c = winLines[l, 2];

            // Đọc giá trị local (đã sync qua NetworkVariable)
            if (GetCell(a).Value == winner && GetCell(b).Value == winner && GetCell(c).Value == winner)
            {
                SetCellColor(a, winColor);
                SetCellColor(b, winColor);
                SetCellColor(c, winColor);
                return;
            }
        }
    }

    [ClientRpc]
    void ResetVisualClientRpc()
    {
        // Xóa pieces
        for (int i = 0; i < 9; i++)
        {
            if (pieces[i] != null)
            {
                Destroy(pieces[i]);
                pieces[i] = null;
            }
            SetCellColor(i, new Color(0, 0, 0, 0));
        }

        if (resetButton != null)
            resetButton.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // SPAWN PIECE LOCAL (visual only, không sync)
    // ══════════════════════════════════════════════════════════════════
    void SpawnPieceLocal(int index, int mark)
    {
        if (pieces[index] != null) return; // đã spawn rồi

        Vector3 localPos = CellLocalPos(index);
        localPos.z = -boardDepth / 2f - 0.05f;

        GameObject prefab = mark == 1 ? xPrefab : oPrefab;

        if (prefab != null)
        {
            pieces[index] = Instantiate(prefab);
            pieces[index].transform.SetParent(transform);
            pieces[index].transform.localPosition = localPos;
            pieces[index].transform.localScale    = Vector3.one * (cellSize * 0.55f);
            pieces[index].transform.localRotation = Quaternion.identity;
        }
        else
        {
            pieces[index] = CreateFallbackPiece(mark, localPos);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // BUILD BOARD (Edit Mode only)
    // ══════════════════════════════════════════════════════════════════
    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            // Chỉ destroy scene object, không destroy prefab asset
            if (!UnityEditor.EditorUtility.IsPersistent(child))
                DestroyImmediate(child);
#else
            DestroyImmediate(child);
#endif
        }
        boardBuilt    = false;
        resetButton   = null;
        cellColliders = new GameObject[9];
        pieces        = new GameObject[9];
    }

    void BuildBoard()
    {
        if (boardBuilt) return;
        if (Application.isPlaying) return;
        boardBuilt = true;

        // Nền
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = "Board_BG";
        bg.transform.SetParent(transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale    = new Vector3(cellSize * 3 + 0.2f, cellSize * 3 + 0.2f, boardDepth);
        SetColor(bg, boardColor);
        RemoveCollider(bg);

        // Đường kẻ
        CreateLine("Line_V1", new Vector3(-cellSize / 2f, 0, -boardDepth / 2f - 0.01f), new Vector3(0.05f, cellSize * 3, 0.05f));
        CreateLine("Line_V2", new Vector3( cellSize / 2f, 0, -boardDepth / 2f - 0.01f), new Vector3(0.05f, cellSize * 3, 0.05f));
        CreateLine("Line_H1", new Vector3(0, -cellSize / 2f, -boardDepth / 2f - 0.01f), new Vector3(cellSize * 3, 0.05f, 0.05f));
        CreateLine("Line_H2", new Vector3(0,  cellSize / 2f, -boardDepth / 2f - 0.01f), new Vector3(cellSize * 3, 0.05f, 0.05f));

        // 9 ô
        for (int i = 0; i < 9; i++)
        {
            Vector3 pos = CellLocalPos(i);
            pos.z = -boardDepth / 2f - 0.02f;

            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cell.name = $"Cell_{i}";
            cell.transform.SetParent(transform);
            cell.transform.localPosition = pos;
            cell.transform.localScale    = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1f);
            cell.transform.localRotation = Quaternion.identity;
            cell.layer = LayerMask.NameToLayer("TicTacToe");

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0, 0, 0, 0);
            cell.GetComponent<Renderer>().material = mat;
            cellColliders[i] = cell;
        }

        // Nút Reset
        CreateResetButton();
    }

    void CreateLine(string lineName, Vector3 localPos, Vector3 scale)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = lineName;
        line.transform.SetParent(transform);
        line.transform.localPosition = localPos;
        line.transform.localScale    = scale;
        SetColor(line, lineColor);
        RemoveCollider(line);
    }

    void CreateResetButton()
    {
        resetButton = GameObject.CreatePrimitive(PrimitiveType.Cube);
        resetButton.name = "ResetButton";
        resetButton.transform.SetParent(transform);
        resetButton.transform.localPosition = new Vector3(0, -(cellSize * 1.5f + 0.45f), boardDepth / 2f);
        resetButton.transform.localScale    = new Vector3(cellSize * 1.8f, 0.35f, 0.1f);
        resetButton.layer = LayerMask.NameToLayer("TicTacToe");
        SetColor(resetButton, new Color(0.2f, 0.6f, 0.3f));
    }

    // ══════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════
    Vector3 CellLocalPos(int index)
    {
        int col = index % 3 - 1;
        int row = 1 - index / 3;
        return new Vector3(col * cellSize, row * cellSize, 0);
    }

    int CheckWinner()
    {
        for (int l = 0; l < 8; l++)
        {
            int a = GetCell(winLines[l, 0]).Value;
            int b = GetCell(winLines[l, 1]).Value;
            int c = GetCell(winLines[l, 2]).Value;
            if (a != 0 && a == b && b == c) return a;
        }
        return 0;
    }

    bool IsBoardFull()
    {
        for (int i = 0; i < 9; i++)
            if (GetCell(i).Value == 0) return false;
        return true;
    }

    void SetCellColor(int index, Color color)
    {
        if (cellColliders[index] == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        cellColliders[index].GetComponent<Renderer>().material = mat;
    }

    GameObject CreateFallbackPiece(int mark, Vector3 localPos)
    {
        GameObject root = new GameObject(mark == 1 ? "X_Piece" : "O_Piece");
        root.transform.SetParent(transform);
        root.transform.localPosition = localPos;
        root.transform.localScale    = Vector3.one * (cellSize * 0.55f);

        Color c = mark == 1 ? new Color(0.9f, 0.1f, 0.1f) : new Color(0.1f, 0.4f, 0.9f);

        if (mark == 1)
        {
            CreateCylinder(root.transform, Vector3.zero, new Vector3(0, 0,  45), new Vector3(0.12f, 0.8f, 0.12f), c);
            CreateCylinder(root.transform, Vector3.zero, new Vector3(0, 0, -45), new Vector3(0.12f, 0.8f, 0.12f), c);
        }
        else
        {
            for (int s = 0; s < 8; s++)
            {
                float angle  = s * 45f * Mathf.Deg2Rad;
                float r      = 0.32f;
                Vector3 sPos = new Vector3(Mathf.Sin(angle) * r, Mathf.Cos(angle) * r, 0);
                CreateCylinder(root.transform, sPos, new Vector3(0, 0, s * 45f), new Vector3(0.1f, 0.28f, 0.1f), c);
            }
        }
        return root;
    }

    void CreateCylinder(Transform parent, Vector3 localPos, Vector3 euler, Vector3 scale, Color color)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.transform.SetParent(parent);
        cyl.transform.localPosition    = localPos;
        cyl.transform.localEulerAngles = euler;
        cyl.transform.localScale       = scale;
        RemoveCollider(cyl);
        SetColor(cyl, color);
    }

    void RemoveCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col == null) return;
        if (Application.isPlaying) Destroy(col);
        else DestroyImmediate(col);
    }

    void SetColor(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        rend.material = mat;
    }
}

// ══════════════════════════════════════════════════════════════════════
// CLICK HANDLERS
// ══════════════════════════════════════════════════════════════════════
public class NetCellHandler : MonoBehaviour
{
    public int cellIndex;
    public TicTacToeLocal game;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layer = LayerMask.GetMask("TicTacToe");
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layer))
            if (hit.collider.gameObject == gameObject)
                game?.OnCellClicked(cellIndex);
    }
}

public class NetResetHandler : MonoBehaviour
{
    public TicTacToeLocal game;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layer = LayerMask.GetMask("TicTacToe");
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layer))
            if (hit.collider.gameObject == gameObject)
                game?.OnResetClicked();
    }
}