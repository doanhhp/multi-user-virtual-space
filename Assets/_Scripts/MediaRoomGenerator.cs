using UnityEngine;

public class MediaRoomGenerator : MonoBehaviour
{
    // =====================================================================
    // === CINEMA ROOM =====================================================
    // =====================================================================
    [Header("=== Cinema: Kích thước ===")]
    public float cinemaWidth  = 24f;
    public float cinemaDepth  = 28f;
    public float cinemaHeight = 8f;

    [Header("=== Cinema: Ghế stadium ===")]
    public GameObject seatPrefab;
    public int   seatRows       = 10;
    public int   seatCols       = 10;
    public float seatSpacingX   = 1.8f;
    public float seatSpacingZ   = 2.0f;
    public float stadiumRise    = 0.35f;   // mỗi hàng cao thêm bao nhiêu
    public float aisleWidth     = 1.6f;    // lối đi 2 bên

    [Header("=== Cinema: Màn hình ===")]
    public float screenWidth    = 18f;
    public float screenHeight   = 7f;
    public Material screenMaterial;

    [Header("=== Cinema: Ánh sáng ===")]
    public float cinemaLightIntensity = 0.6f;
    public Color cinemaLightColor     = new Color(0.9f, 0.85f, 0.75f);

    // =====================================================================
    // === WAITING ROOM ====================================================
    // =====================================================================
    [Header("=== Waiting Room: Kích thước ===")]
    public float loungeWidth  = 20f;
    public float loungeDepth  = 20f;
    public float loungeHeight = 4f;

    [Header("=== Waiting Room: Sofa ===")]
    public float sofaArmLength  = 6f;   // chiều dài mỗi cánh sofa
    public float sofaWidth      = 1.2f;
    public float sofaHeight     = 0.5f;

    [Header("=== Spawn Points ===")]
    public int cinemaSpawnCount = 4;
    public int loungeSpawnCount = 4;

    // =====================================================================
    // ENTRY POINT
    // =====================================================================
    public void GenerateRoom()
    {
        ClearChildren();
        GenerateCinemaRoom();
        GenerateWaitingRoom();
        GenerateDoors();
        Debug.Log("[MediaRoomGenerator] Cả hai phòng đã được tạo thành công!");
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    // =====================================================================
    // CINEMA ROOM
    // =====================================================================
    void GenerateCinemaRoom()
    {
        GameObject cinema = new GameObject("CinemaRoom");
        cinema.transform.SetParent(transform);
        cinema.transform.localPosition = Vector3.zero;

        CreateCinemaFloor(cinema.transform);
        CreateCinemaWalls(cinema.transform);
        CreateCinemaCeiling(cinema.transform);
        CreateCinemaScreen(cinema.transform);
        CreateStadiumSeats(cinema.transform);
        CreateCinemaLights(cinema.transform);
        CreateCinemaSpawnPoints(cinema.transform);
    }

    void CreateCinemaFloor(Transform parent)
    {
        // Sàn thảm chính
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_Carpet";
        floor.transform.SetParent(parent);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = new Vector3(cinemaWidth / 10f, 1f, cinemaDepth / 10f);
        SetMaterialColor(floor, new Color(0.55f, 0.12f, 0.08f)); // đỏ cam thảm
    }

    void CreateCinemaWalls(Transform parent)
    {
        // Tường trước (màn hình)
        CreateWallWithCollider(parent, "Wall_Front",
            new Vector3(0, cinemaHeight / 2f, cinemaDepth / 2f),
            new Vector3(cinemaWidth, cinemaHeight, 0.3f),
            new Color(0.1f, 0.1f, 0.12f));

        // Tường sau
        CreateWallWithCollider(parent, "Wall_Back",
            new Vector3(0, cinemaHeight / 2f, -cinemaDepth / 2f),
            new Vector3(cinemaWidth, cinemaHeight, 0.3f),
            new Color(0.1f, 0.1f, 0.12f));

        // Tường trái — acoustic panels màu nâu đỏ
        CreateAcousticWall(parent, "Wall_Left",
            new Vector3(-cinemaWidth / 2f, cinemaHeight / 2f, 0),
            new Vector3(0.3f, cinemaHeight, cinemaDepth),
            true);

        // Tường phải — acoustic panels
        CreateAcousticWall(parent, "Wall_Right",
            new Vector3(cinemaWidth / 2f, cinemaHeight / 2f, 0),
            new Vector3(0.3f, cinemaHeight, cinemaDepth),
            false);
    }

    void CreateAcousticWall(Transform parent, string wallName, Vector3 pos, Vector3 size, bool isLeft)
    {
        // Tường base
        CreateWallWithCollider(parent, wallName, pos, size, new Color(0.18f, 0.08f, 0.06f));

        // Acoustic panels xếp đều (hình chữ nhật)
        int panelCols = 6;
        int panelRows = 3;
        float panelW = 1.4f;
        float panelH = 1.8f;
        float panelDepth = 0.08f;
        float spacingZ = cinemaDepth / (panelCols + 1);
        float spacingY = cinemaHeight / (panelRows + 1);

        for (int c = 0; c < panelCols; c++)
        {
            for (int r = 0; r < panelRows; r++)
            {
                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = $"AcousticPanel_{c}_{r}";
                panel.transform.SetParent(parent);

                float pz = -cinemaDepth / 2f + spacingZ * (c + 1);
                float py = spacingY * (r + 1);
                float px = isLeft
                    ? -cinemaWidth / 2f + panelDepth
                    :  cinemaWidth / 2f - panelDepth;

                panel.transform.localPosition = new Vector3(px, py, pz);
                panel.transform.localScale = isLeft
                    ? new Vector3(panelDepth, panelH, panelW)
                    : new Vector3(panelDepth, panelH, panelW);

                DestroyImmediate(panel.GetComponent<Collider>());
                SetMaterialColor(panel, new Color(0.45f, 0.18f, 0.12f)); // nâu đỏ
            }
        }
    }

    void CreateCinemaCeiling(Transform parent)
    {
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(parent);
        ceiling.transform.localPosition = new Vector3(0, cinemaHeight, 0);
        ceiling.transform.localScale = new Vector3(cinemaWidth, 0.3f, cinemaDepth);
        SetMaterialColor(ceiling, new Color(0.05f, 0.05f, 0.05f)); // đen
        // Không cần collider trần
        DestroyImmediate(ceiling.GetComponent<Collider>());
    }

    void CreateCinemaScreen(Transform parent)
    {
        // Viền đen quanh màn hình
        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.name = "Screen_Border";
        border.transform.SetParent(parent);
        border.transform.localPosition = new Vector3(0, screenHeight / 2f + 1.2f, cinemaDepth / 2f - 0.15f);
        border.transform.localScale = new Vector3(screenWidth + 0.6f, screenHeight + 0.6f, 0.1f);
        SetMaterialColor(border, new Color(0.05f, 0.05f, 0.05f));
        DestroyImmediate(border.GetComponent<Collider>());

        // Màn hình trắng
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "MediaBoard_Screen";
        screen.transform.SetParent(parent);
        screen.transform.localPosition = new Vector3(0, screenHeight / 2f + 1.2f, cinemaDepth / 2f - 0.2f);
        screen.transform.localScale = new Vector3(screenWidth, screenHeight, 1f);
        screen.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        if (screenMaterial != null)
            screen.GetComponent<Renderer>().material = screenMaterial;
        else
            SetMaterialColor(screen, new Color(0.95f, 0.95f, 0.95f));

        // Platform trước màn hình
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Screen_Platform";
        platform.transform.SetParent(parent);
        platform.transform.localPosition = new Vector3(0, 0.15f, cinemaDepth / 2f - 1.5f);
        platform.transform.localScale = new Vector3(cinemaWidth - 0.6f, 0.3f, 2.5f);
        SetMaterialColor(platform, new Color(0.12f, 0.12f, 0.14f));

        // Loa trái
        CreateSpeaker(parent, new Vector3(-screenWidth / 2f - 0.8f, screenHeight / 2f + 0.5f, cinemaDepth / 2f - 0.3f));
        // Loa phải
        CreateSpeaker(parent, new Vector3( screenWidth / 2f + 0.8f, screenHeight / 2f + 0.5f, cinemaDepth / 2f - 0.3f));
    }

    void CreateSpeaker(Transform parent, Vector3 pos)
    {
        GameObject spk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spk.name = "Speaker";
        spk.transform.SetParent(parent);
        spk.transform.localPosition = pos;
        spk.transform.localScale = new Vector3(0.4f, 0.8f, 0.25f);
        SetMaterialColor(spk, new Color(0.08f, 0.08f, 0.08f));
        DestroyImmediate(spk.GetComponent<Collider>());
    }

    void CreateStadiumSeats(Transform parent)
    {
        GameObject seatsParent = new GameObject("Seats");
        seatsParent.transform.SetParent(parent);

        // Tổng width của 1 block ghế (5 ghế mỗi bên lối đi)
        int halfCols = seatCols / 2;
        float blockWidth = halfCols * seatSpacingX;

        // startZ: hàng đầu cách platform 1.5m
        float startZ = cinemaDepth / 2f - 4.5f;

        for (int row = 0; row < seatRows; row++)
        {
            float posZ    = startZ - row * seatSpacingZ;
            float riseY   = row * stadiumRise;           // cao dần
            float stepH   = row * stadiumRise;           // bậc thang tương ứng

            // Tạo bậc thang cho hàng này (từ hàng 1 trở đi)
            if (row > 0)
                CreateStadiumStep(seatsParent, posZ, riseY, row);

            for (int col = 0; col < seatCols; col++)
            {
                // Tính vị trí X — chừa lối đi ở giữa
                float colOffset;
                if (col < halfCols)
                    colOffset = -(aisleWidth / 2f) - (halfCols - 1 - col) * seatSpacingX - seatSpacingX / 2f;
                else
                    colOffset =  (aisleWidth / 2f) + (col - halfCols) * seatSpacingX + seatSpacingX / 2f;

                Vector3 seatPos = new Vector3(colOffset, riseY, posZ);

                if (seatPrefab != null)
                {
                    GameObject seat = Instantiate(seatPrefab, Vector3.zero, Quaternion.Euler(0, 180f, 0));
                    seat.name = $"Seat_R{row + 1}_C{col + 1}";
                    seat.transform.SetParent(seatsParent.transform);
                    seat.transform.localPosition = seatPos;
                }
                else
                {
                    CreateSimpleSeat(seatsParent.transform, seatPos, row, col);
                }
            }
        }

        // Lối đi giữa (2 bên)
        CreateAisle(seatsParent, true,  startZ);
        CreateAisle(seatsParent, false, startZ);
    }

    void CreateStadiumStep(GameObject parent, float posZ, float riseY, int row)
    {
        // Bậc thang (collider để nhân vật đứng được)
        GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
        step.name = $"Step_Row{row + 1}";
        step.transform.SetParent(parent.transform);
        float stepHeight = stadiumRise;
        step.transform.localPosition = new Vector3(0, riseY - stepHeight / 2f, posZ + seatSpacingZ / 2f);
        step.transform.localScale = new Vector3(cinemaWidth - 1f, stepHeight, seatSpacingZ);
        SetMaterialColor(step, new Color(0.18f, 0.08f, 0.06f)); // màu thảm

        // Đèn dọc bậc thang (trái)
        CreateStepLight(parent.transform, new Vector3(-cinemaWidth / 2f + 0.3f, riseY + 0.1f, posZ + seatSpacingZ / 2f));
        // Đèn dọc bậc thang (phải)
        CreateStepLight(parent.transform, new Vector3( cinemaWidth / 2f - 0.3f, riseY + 0.1f, posZ + seatSpacingZ / 2f));
    }

    void CreateStepLight(Transform parent, Vector3 pos)
    {
        GameObject lightObj = new GameObject("StepLight");
        lightObj.transform.SetParent(parent);
        lightObj.transform.localPosition = pos;

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.8f, 0.6f, 0.3f);
        l.intensity = 0.4f;
        l.range = 1.5f;

        // Bulb nhỏ
        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "StepBulb";
        bulb.transform.SetParent(lightObj.transform);
        bulb.transform.localPosition = Vector3.zero;
        bulb.transform.localScale = Vector3.one * 0.06f;
        DestroyImmediate(bulb.GetComponent<Collider>());
        SetMaterialColor(bulb, new Color(1f, 0.9f, 0.5f));
    }

    void CreateAisle(GameObject parent, bool isLeft, float startZ)
    {
        float aisleX = isLeft ? -(aisleWidth / 2f) - 0.1f : aisleWidth / 2f + 0.1f;
        float totalDepth = seatRows * seatSpacingZ;

        GameObject aisle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        aisle.name = isLeft ? "Aisle_Left" : "Aisle_Right";
        aisle.transform.SetParent(parent.transform);
        aisle.transform.localPosition = new Vector3(aisleX, -0.01f, startZ - totalDepth / 2f);
        aisle.transform.localScale = new Vector3(aisleWidth, 0.02f, totalDepth);
        SetMaterialColor(aisle, new Color(0.3f, 0.08f, 0.05f));
        // Giữ collider để nhân vật đi trên lối đi
    }

    void CreateSimpleSeat(Transform parent, Vector3 localPos, int row, int col)
    {
        GameObject seatRoot = new GameObject($"Seat_R{row + 1}_C{col + 1}");
        seatRoot.transform.SetParent(parent);
        seatRoot.transform.localPosition = localPos;

        // Đệm ngồi — đỏ
        GameObject cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cushion.name = "Cushion";
        cushion.transform.SetParent(seatRoot.transform);
        cushion.transform.localPosition = new Vector3(0, 0.45f, 0.05f);
        cushion.transform.localScale = new Vector3(0.85f, 0.12f, 0.75f);
        SetMaterialColor(cushion, new Color(0.7f, 0.08f, 0.08f)); // đỏ

        // Lưng ghế
        GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backrest.name = "Backrest";
        backrest.transform.SetParent(seatRoot.transform);
        backrest.transform.localPosition = new Vector3(0, 0.78f, -0.32f);
        backrest.transform.localScale = new Vector3(0.85f, 0.65f, 0.12f);
        SetMaterialColor(backrest, new Color(0.65f, 0.07f, 0.07f));

        // Chân ghế — đen
        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leg.name = "Leg";
        leg.transform.SetParent(seatRoot.transform);
        leg.transform.localPosition = new Vector3(0, 0.22f, -0.05f);
        leg.transform.localScale = new Vector3(0.12f, 0.44f, 0.12f);
        SetMaterialColor(leg, new Color(0.1f, 0.1f, 0.1f));

        // Tay vịn trái
        CreateArmrest(seatRoot.transform, -0.46f);
        // Tay vịn phải
        CreateArmrest(seatRoot.transform,  0.46f);
    }

    void CreateArmrest(Transform parent, float x)
    {
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Armrest";
        arm.transform.SetParent(parent);
        arm.transform.localPosition = new Vector3(x, 0.58f, -0.05f);
        arm.transform.localScale = new Vector3(0.08f, 0.12f, 0.7f);
        SetMaterialColor(arm, new Color(0.1f, 0.1f, 0.1f));
        DestroyImmediate(arm.GetComponent<Collider>());
    }

    void CreateCinemaLights(Transform parent)
    {
        GameObject lightsParent = new GameObject("Lights");
        lightsParent.transform.SetParent(parent);

        // Downlights âm trần — 3 hàng x 4 cột
        int lRows = 3, lCols = 4;
        float spX = cinemaWidth  / (lCols + 1);
        float spZ = cinemaDepth  / (lRows + 1);

        for (int r = 0; r < lRows; r++)
        {
            for (int c = 0; c < lCols; c++)
            {
                float px = -cinemaWidth / 2f  + spX * (c + 1);
                float pz = -cinemaDepth / 2f  + spZ * (r + 1);

                GameObject lo = new GameObject($"Downlight_{r}_{c}");
                lo.transform.SetParent(lightsParent.transform);
                lo.transform.localPosition = new Vector3(px, cinemaHeight - 0.1f, pz);

                Light l = lo.AddComponent<Light>();
                l.type = LightType.Spot;
                l.color = cinemaLightColor;
                l.intensity = cinemaLightIntensity;
                l.range = cinemaHeight + 3f;
                l.spotAngle = 50f;
                l.transform.localRotation = Quaternion.Euler(90f, 0, 0);

                // Housing âm trần
                GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                housing.name = "Housing";
                housing.transform.SetParent(lo.transform);
                housing.transform.localPosition = Vector3.zero;
                housing.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
                DestroyImmediate(housing.GetComponent<Collider>());
                SetMaterialColor(housing, new Color(0.15f, 0.15f, 0.15f));
            }
        }

        // Spotlight màn hình
        GameObject spot = new GameObject("Spotlight_Screen");
        spot.transform.SetParent(lightsParent.transform);
        spot.transform.localPosition = new Vector3(0, cinemaHeight - 0.5f, cinemaDepth / 2f - 4f);
        spot.transform.localRotation = Quaternion.Euler(25f, 0, 0);
        Light sl = spot.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = Color.white;
        sl.intensity = 1.5f;
        sl.range = 20f;
        sl.spotAngle = 55f;
    }

    void CreateCinemaSpawnPoints(Transform parent)
    {
        GameObject sp = new GameObject("SpawnPoints");
        sp.transform.SetParent(parent);
        float spacingX = cinemaWidth / (cinemaSpawnCount + 1);
        for (int i = 0; i < cinemaSpawnCount; i++)
        {
            GameObject s = new GameObject($"CinemaSpawn_{i + 1}");
            s.transform.SetParent(sp.transform);
            s.transform.localPosition = new Vector3(
                -cinemaWidth / 2f + spacingX * (i + 1), 0.1f, -cinemaDepth / 2f + 2f);
        }
    }

    // =====================================================================
    // WAITING / LOUNGE ROOM
    // =====================================================================
    void GenerateWaitingRoom()
    {
        GameObject lounge = new GameObject("WaitingRoom");
        lounge.transform.SetParent(transform);
        // Phòng chờ nằm bên hông phải phòng chiếu
        float offsetX = cinemaWidth / 2f + loungeWidth / 2f;
        lounge.transform.localPosition = new Vector3(offsetX, 0, 0);

        CreateLoungeFloor(lounge.transform);
        CreateLoungeWalls(lounge.transform);
        CreateLoungeCeiling(lounge.transform);
        CreateSofaPlus(lounge.transform);
        CreateWaterBar(lounge.transform);
        CreateBoardGameWall(lounge.transform);
        CreateLoungeLights(lounge.transform);
        CreateLoungeSpawnPoints(lounge.transform);
    }

    void CreateLoungeFloor(Transform parent)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = new Vector3(loungeWidth / 10f, 1f, loungeDepth / 10f);
        SetMaterialColor(floor, new Color(0.72f, 0.72f, 0.72f)); // xám sáng
    }

    void CreateLoungeWalls(Transform parent)
    {
        // Tường trước
        CreateWallWithCollider(parent, "Wall_Front",
            new Vector3(0, loungeHeight / 2f, loungeDepth / 2f),
            new Vector3(loungeWidth, loungeHeight, 0.3f),
            new Color(0.92f, 0.90f, 0.86f));
        // Tường sau (có cửa — tường chia đôi)
        CreateWallWithCollider(parent, "Wall_Back_L",
            new Vector3(-loungeWidth / 4f - 0.75f, loungeHeight / 2f, -loungeDepth / 2f),
            new Vector3(loungeWidth / 2f - 1.5f, loungeHeight, 0.3f),
            new Color(0.92f, 0.90f, 0.86f));
        CreateWallWithCollider(parent, "Wall_Back_R",
            new Vector3( loungeWidth / 4f + 0.75f, loungeHeight / 2f, -loungeDepth / 2f),
            new Vector3(loungeWidth / 2f - 1.5f, loungeHeight, 0.3f),
            new Color(0.92f, 0.90f, 0.86f));
        // Tường trái (tiếp giáp cinema — có cửa, xử lý ở GenerateDoors)
        CreateWallWithCollider(parent, "Wall_Left_Top",
            new Vector3(-loungeWidth / 2f, loungeHeight * 0.75f, 0),
            new Vector3(0.3f, loungeHeight * 0.5f, loungeDepth),
            new Color(0.92f, 0.90f, 0.86f));
        // Tường phải
        CreateWallWithCollider(parent, "Wall_Right",
            new Vector3(loungeWidth / 2f, loungeHeight / 2f, 0),
            new Vector3(0.3f, loungeHeight, loungeDepth),
            new Color(0.92f, 0.90f, 0.86f));
    }

    void CreateLoungeCeiling(Transform parent)
    {
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(parent);
        ceiling.transform.localPosition = new Vector3(0, loungeHeight, 0);
        ceiling.transform.localScale = new Vector3(loungeWidth, 0.3f, loungeDepth);
        SetMaterialColor(ceiling, new Color(0.95f, 0.95f, 0.95f));
        DestroyImmediate(ceiling.GetComponent<Collider>());
    }

    // Sofa hình dấu + ở giữa phòng
    void CreateSofaPlus(Transform parent)
    {
        GameObject sofaParent = new GameObject("SofaSet_Plus");
        sofaParent.transform.SetParent(parent);
        sofaParent.transform.localPosition = new Vector3(0, 0, 0);

        // 4 cánh: North, South, East, West
        // North
        CreateSofaArm(sofaParent.transform, "Sofa_North",
            new Vector3(0, 0, sofaArmLength / 2f + sofaWidth / 2f),
            new Vector3(sofaWidth, sofaHeight, sofaArmLength),
            Quaternion.identity);
        // South
        CreateSofaArm(sofaParent.transform, "Sofa_South",
            new Vector3(0, 0, -(sofaArmLength / 2f + sofaWidth / 2f)),
            new Vector3(sofaWidth, sofaHeight, sofaArmLength),
            Quaternion.identity);
        // East
        CreateSofaArm(sofaParent.transform, "Sofa_East",
            new Vector3(sofaArmLength / 2f + sofaWidth / 2f, 0, 0),
            new Vector3(sofaArmLength, sofaHeight, sofaWidth),
            Quaternion.identity);
        // West
        CreateSofaArm(sofaParent.transform, "Sofa_West",
            new Vector3(-(sofaArmLength / 2f + sofaWidth / 2f), 0, 0),
            new Vector3(sofaArmLength, sofaHeight, sofaWidth),
            Quaternion.identity);

        // Trung tâm
        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cube);
        center.name = "Sofa_Center";
        center.transform.SetParent(sofaParent.transform);
        center.transform.localPosition = new Vector3(0, sofaHeight / 2f, 0);
        center.transform.localScale = new Vector3(sofaWidth + 0.2f, sofaHeight, sofaWidth + 0.2f);
        SetMaterialColor(center, new Color(0.85f, 0.45f, 0.15f)); // cam

        // Bàn nhỏ đầu mỗi cánh sofa
        CreateSofaTable(sofaParent.transform, new Vector3(0, 0,  sofaArmLength + sofaWidth + 0.6f));
        CreateSofaTable(sofaParent.transform, new Vector3(0, 0, -(sofaArmLength + sofaWidth + 0.6f)));
        CreateSofaTable(sofaParent.transform, new Vector3( sofaArmLength + sofaWidth + 0.6f, 0, 0));
        CreateSofaTable(sofaParent.transform, new Vector3(-(sofaArmLength + sofaWidth + 0.6f), 0, 0));

        // Pouf nhỏ (ghế cube nhỏ màu vàng & cam)
        CreatePouf(sofaParent.transform, new Vector3( 2.5f, 0,  2.5f), new Color(0.9f, 0.8f, 0.1f));
        CreatePouf(sofaParent.transform, new Vector3(-2.5f, 0, -2.5f), new Color(0.9f, 0.8f, 0.1f));
        CreatePouf(sofaParent.transform, new Vector3( 2.5f, 0, -2.5f), new Color(0.85f, 0.35f, 0.1f));
        CreatePouf(sofaParent.transform, new Vector3(-2.5f, 0,  2.5f), new Color(0.85f, 0.35f, 0.1f));
    }

    void CreateSofaArm(Transform parent, string sofaName, Vector3 pos, Vector3 size, Quaternion rot)
    {
        GameObject sofa = new GameObject(sofaName);
        sofa.transform.SetParent(parent);
        sofa.transform.localPosition = pos;
        sofa.transform.localRotation = rot;

        // Đệm ngồi
        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.name = "Seat";
        seat.transform.SetParent(sofa.transform);
        seat.transform.localPosition = new Vector3(0, sofaHeight / 2f, 0);
        seat.transform.localScale = size;
        SetMaterialColor(seat, new Color(0.92f, 0.90f, 0.86f)); // trắng kem

        // Lưng sofa (dọc theo chiều dài)
        bool isNS = size.x < size.z; // North/South thì z dài hơn
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "Back";
        back.transform.SetParent(sofa.transform);
        back.transform.localPosition = new Vector3(0, sofaHeight / 2f + 0.3f, 0);
        back.transform.localScale = isNS
            ? new Vector3(size.x, 0.5f, 0.15f)
            : new Vector3(0.15f, 0.5f, size.z);
        SetMaterialColor(back, new Color(0.35f, 0.35f, 0.38f)); // xám tối
        DestroyImmediate(back.GetComponent<Collider>());
    }

    void CreateSofaTable(Transform parent, Vector3 pos)
    {
        GameObject table = new GameObject("SideTable");
        table.transform.SetParent(parent);
        table.transform.localPosition = pos;

        // Mặt bàn
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        top.name = "TableTop";
        top.transform.SetParent(table.transform);
        top.transform.localPosition = new Vector3(0, 0.55f, 0);
        top.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
        SetMaterialColor(top, new Color(0.95f, 0.95f, 0.95f));

        // Chân bàn
        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leg.name = "TableLeg";
        leg.transform.SetParent(table.transform);
        leg.transform.localPosition = new Vector3(0, 0.28f, 0);
        leg.transform.localScale = new Vector3(0.06f, 0.28f, 0.06f);
        SetMaterialColor(leg, new Color(0.8f, 0.8f, 0.8f));
        DestroyImmediate(leg.GetComponent<Collider>());
    }

    void CreatePouf(Transform parent, Vector3 pos, Color color)
    {
        GameObject pouf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pouf.name = "Pouf";
        pouf.transform.SetParent(parent);
        pouf.transform.localPosition = new Vector3(pos.x, 0.2f, pos.z);
        pouf.transform.localScale = new Vector3(0.55f, 0.4f, 0.55f);
        SetMaterialColor(pouf, color);
    }

    void CreateWaterBar(Transform parent)
    {
        // Quầy nước — góc trái phía trước
        GameObject bar = new GameObject("WaterBar");
        bar.transform.SetParent(parent);
        bar.transform.localPosition = new Vector3(-loungeWidth / 2f + 2.5f, 0, loungeDepth / 2f - 2f);

        // Quầy bar
        GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        counter.name = "Counter";
        counter.transform.SetParent(bar.transform);
        counter.transform.localPosition = new Vector3(0, 0.5f, 0);
        counter.transform.localScale = new Vector3(3.5f, 1.0f, 0.8f);
        SetMaterialColor(counter, new Color(0.85f, 0.78f, 0.65f)); // gỗ sáng

        // Mặt quầy
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "CounterTop";
        top.transform.SetParent(bar.transform);
        top.transform.localPosition = new Vector3(0, 1.02f, 0);
        top.transform.localScale = new Vector3(3.6f, 0.06f, 0.9f);
        SetMaterialColor(top, new Color(0.3f, 0.22f, 0.15f)); // gỗ tối

        // Kệ đồ phía sau
        GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shelf.name = "Shelf";
        shelf.transform.SetParent(bar.transform);
        shelf.transform.localPosition = new Vector3(0, 1.5f, -0.5f);
        shelf.transform.localScale = new Vector3(3.5f, 0.08f, 0.4f);
        SetMaterialColor(shelf, new Color(0.85f, 0.78f, 0.65f));
        DestroyImmediate(shelf.GetComponent<Collider>());

        // Chai/bình trang trí nhỏ
        for (int i = 0; i < 4; i++)
        {
            GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.name = $"Bottle_{i}";
            bottle.transform.SetParent(bar.transform);
            bottle.transform.localPosition = new Vector3(-1.2f + i * 0.8f, 1.65f, -0.5f);
            bottle.transform.localScale = new Vector3(0.1f, 0.18f, 0.1f);
            SetMaterialColor(bottle, new Color(0.2f + i * 0.1f, 0.4f, 0.6f));
            DestroyImmediate(bottle.GetComponent<Collider>());
        }

        // Ghế bar
        for (int i = 0; i < 3; i++)
        {
            GameObject stool = new GameObject($"BarStool_{i}");
            stool.transform.SetParent(bar.transform);
            stool.transform.localPosition = new Vector3(-1f + i * 1f, 0, 0.9f);

            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seat.name = "Seat";
            seat.transform.SetParent(stool.transform);
            seat.transform.localPosition = new Vector3(0, 0.7f, 0);
            seat.transform.localScale = new Vector3(0.35f, 0.06f, 0.35f);
            SetMaterialColor(seat, new Color(0.3f, 0.22f, 0.15f));

            GameObject sleg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleg.name = "Leg";
            sleg.transform.SetParent(stool.transform);
            sleg.transform.localPosition = new Vector3(0, 0.35f, 0);
            sleg.transform.localScale = new Vector3(0.06f, 0.35f, 0.06f);
            SetMaterialColor(sleg, new Color(0.7f, 0.7f, 0.7f));
            DestroyImmediate(sleg.GetComponent<Collider>());
        }
    }

    void CreateBoardGameWall(Transform parent)
    {
        // Bảng treo tường — tường phải, giữa phòng
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "BoardGame_Wall";
        board.transform.SetParent(parent);
        board.transform.localPosition = new Vector3(loungeWidth / 2f - 0.1f, loungeHeight * 0.55f, 0);
        board.transform.localScale = new Vector3(0.1f, 2.5f, 4f);
        SetMaterialColor(board, new Color(0.2f, 0.55f, 0.3f)); // xanh lá bảng

        // Viền bảng
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Board_Frame";
        frame.transform.SetParent(parent);
        frame.transform.localPosition = new Vector3(loungeWidth / 2f - 0.12f, loungeHeight * 0.55f, 0);
        frame.transform.localScale = new Vector3(0.08f, 2.7f, 4.2f);
        SetMaterialColor(frame, new Color(0.3f, 0.22f, 0.15f));
        DestroyImmediate(frame.GetComponent<Collider>());
    }

    void CreateLoungeLights(Transform parent)
    {
        GameObject lightsParent = new GameObject("Lights");
        lightsParent.transform.SetParent(parent);

        int lRows = 2, lCols = 3;
        float spX = loungeWidth  / (lCols + 1);
        float spZ = loungeDepth  / (lRows + 1);

        for (int r = 0; r < lRows; r++)
        {
            for (int c = 0; c < lCols; c++)
            {
                float px = -loungeWidth / 2f + spX * (c + 1);
                float pz = -loungeDepth / 2f + spZ * (r + 1);

                GameObject lo = new GameObject($"Light_{r}_{c}");
                lo.transform.SetParent(lightsParent.transform);
                lo.transform.localPosition = new Vector3(px, loungeHeight - 0.1f, pz);

                Light l = lo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.95f, 0.85f);
                l.intensity = 1.2f;
                l.range = Mathf.Max(loungeWidth, loungeDepth) * 0.6f;

                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Bulb";
                bulb.transform.SetParent(lo.transform);
                bulb.transform.localPosition = Vector3.zero;
                bulb.transform.localScale = Vector3.one * 0.12f;
                DestroyImmediate(bulb.GetComponent<Collider>());
                SetMaterialColor(bulb, new Color(1f, 0.98f, 0.85f));
            }
        }
    }

    void CreateLoungeSpawnPoints(Transform parent)
    {
        GameObject sp = new GameObject("SpawnPoints");
        sp.transform.SetParent(parent);
        float spX = loungeWidth / (loungeSpawnCount + 1);
        for (int i = 0; i < loungeSpawnCount; i++)
        {
            GameObject s = new GameObject($"LoungeSpawn_{i + 1}");
            s.transform.SetParent(sp.transform);
            s.transform.localPosition = new Vector3(
                -loungeWidth / 2f + spX * (i + 1), 0.1f, 0);
        }
    }

    // =====================================================================
    // DOORS — nối hai phòng
    // =====================================================================
    void GenerateDoors()
    {
        // Cửa nằm trên tường chung giữa Cinema (Wall_Right) và Lounge (Wall_Left)
        // Vị trí X = cinemaWidth/2, Z = 0 (giữa phòng)
        float doorX  = cinemaWidth / 2f;
        float doorW  = 1.4f;
        float doorH  = 2.4f;

        // Phần tường trên cửa phía Cinema
        CreateDoorSegment("Door_CinemaWall_Top",
            new Vector3(doorX, cinemaHeight - (cinemaHeight - doorH) / 2f, 0),
            new Vector3(0.3f, cinemaHeight - doorH, doorW + 0.6f),
            new Color(0.1f, 0.1f, 0.12f));

        // Phần tường trên cửa phía Lounge (local space của lounge = X = -loungeWidth/2)
        float loungeOffsetX = cinemaWidth / 2f + loungeWidth / 2f;
        CreateDoorSegment("Door_LoungeWall_Top",
            new Vector3(loungeOffsetX - loungeWidth / 2f, loungeHeight - (loungeHeight - doorH) / 2f, 0),
            new Vector3(0.3f, loungeHeight - doorH, doorW + 0.6f),
            new Color(0.92f, 0.90f, 0.86f));

        // Cánh cửa (có thể swap prefab sau)
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door";
        door.transform.SetParent(transform);
        door.transform.localPosition = new Vector3(doorX, doorH / 2f, 0);
        door.transform.localScale = new Vector3(0.1f, doorH, doorW);
        SetMaterialColor(door, new Color(0.5f, 0.35f, 0.2f)); // gỗ

        // Tay nắm
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "DoorHandle";
        handle.transform.SetParent(door.transform);
        handle.transform.localPosition = new Vector3(0.8f, -0.05f, 0.3f);
        handle.transform.localScale = new Vector3(0.05f, 0.12f, 0.05f);
        handle.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        SetMaterialColor(handle, new Color(0.75f, 0.65f, 0.1f)); // vàng
        DestroyImmediate(handle.GetComponent<Collider>());
    }

    void CreateDoorSegment(string name, Vector3 pos, Vector3 size, Color color)
    {
        GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seg.name = name;
        seg.transform.SetParent(transform);
        seg.transform.localPosition = pos;
        seg.transform.localScale = size;
        SetMaterialColor(seg, color);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================
    void CreateWallWithCollider(Transform parent, string wallName, Vector3 localPos, Vector3 size, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = size;
        SetMaterialColor(wall, color);
        // Box Collider tự sinh bởi CreatePrimitive — giữ nguyên
    }

    void SetMaterialColor(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
        if (shader == null) return;
        Material mat = new Material(shader);
        mat.color = color;
        rend.material = mat;
    }
}