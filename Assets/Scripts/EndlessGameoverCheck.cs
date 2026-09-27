using UnityEngine;
using UnityEngine.Events;

public class EndlessGameoverCheck : MonoBehaviour
{
    public UnityEvent OnGameOver;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("gege");
        if (collision.gameObject.tag != "Player") return;
        OnGameOver?.Invoke();
    }
}
