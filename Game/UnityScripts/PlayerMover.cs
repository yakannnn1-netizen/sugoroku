using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Game.Core;

public class PlayerMover : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5.0f;
    public float hopHeight = 1.0f; // チョコボランド風の跳ねる動き

    private BoardRenderer boardRenderer;
    private Player ownerPlayer;

    public void Initialize(Player player, BoardRenderer renderer, Square startSquare)
    {
        ownerPlayer = player;
        boardRenderer = renderer;

        // 初期位置にスナップ
        transform.position = boardRenderer.GetSquarePosition(startSquare);
    }

    // Taskを返すことで、GameManagerのMovePlayerAsyncに合わせて移動の完了を待機可能にする
    public Task MoveToSquareAsync(Square targetSquare)
    {
        var tcs = new TaskCompletionSource<bool>();
        Vector3 targetPosition = boardRenderer.GetSquarePosition(targetSquare);

        StartCoroutine(MoveCoroutine(targetPosition, tcs));
        return tcs.Task;
    }

    private IEnumerator MoveCoroutine(Vector3 targetPosition, TaskCompletionSource<bool> tcs)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / moveSpeed;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            // 直線補間
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, normalizedTime);

            // 跳ねるような動き（サイン波）をY軸に加える
            float hopY = Mathf.Sin(normalizedTime * Mathf.PI) * hopHeight;
            currentPos.y += hopY;

            transform.position = currentPos;

            // 進行方向を向く
            Vector3 direction = (targetPosition - startPosition).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            yield return null;
        }

        transform.position = targetPosition;
        tcs.SetResult(true);
    }
}
