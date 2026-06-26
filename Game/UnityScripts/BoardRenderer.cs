using System.Collections.Generic;
using UnityEngine;
using Game.Core;

public class BoardRenderer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject startSquarePrefab;
    public GameObject propertySquarePrefab;
    public GameObject shopSquarePrefab;
    public GameObject checkpointSquarePrefab;

    [Header("Settings")]
    public float squareSpacing = 2.0f; // マス同士の間隔

    // Core側のSquareとUnity上のGameObjectを紐付ける辞書
    private Dictionary<Square, GameObject> squareViews = new Dictionary<Square, GameObject>();

    public void RenderBoard(Board board)
    {
        // 簡易的な円形配置のアルゴリズム（実際のマップ構造に応じて拡張してください）
        int totalSquares = board.AllSquares.Count;
        float radius = totalSquares * squareSpacing / (2 * Mathf.PI);

        for (int i = 0; i < totalSquares; i++)
        {
            var square = board.AllSquares[i];

            // マスの種類に応じてプレハブを選択
            GameObject prefabToUse = propertySquarePrefab; // デフォルト
            if (square is StartSquare) prefabToUse = startSquarePrefab;
            else if (square is CheckpointSquare) prefabToUse = checkpointSquarePrefab;
            else if (square is ShopSquare) prefabToUse = shopSquarePrefab;

            // 円形に配置
            float angle = i * Mathf.PI * 2 / totalSquares;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            // インスタンス化
            GameObject squareObj = Instantiate(prefabToUse, position, Quaternion.identity, this.transform);
            squareObj.name = $"Square_{square.Id}_{square.Name}";

            // 表示名の設定（3D TextやUIキャンバス等があれば）
            var textMesh = squareObj.GetComponentInChildren<TextMesh>();
            if (textMesh != null)
            {
                textMesh.text = square.Name;
            }

            squareViews[square] = squareObj;
        }
    }

    // 指定したマスのワールド座標を取得する
    public Vector3 GetSquarePosition(Square square)
    {
        if (squareViews.TryGetValue(square, out GameObject obj))
        {
            return obj.transform.position;
        }
        return Vector3.zero;
    }
}
