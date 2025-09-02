using UnityEngine;
using System.Collections;
using NaughtyAttributes;
public class MovingEffect : ObjectEffect
{
    public enum MovingType
    {
        FromCurrentPosition,
        ToDestinatePosition
    }
    public MovingType type;
    [SerializeField,ShowIf("type",MovingType.ToDestinatePosition)] private Vector3 destinatePosition;
    [SerializeField, ShowIf("type", MovingType.FromCurrentPosition)] private Vector3 offsetPosition;
    private Vector3 startPosition;
    private Vector3 targetPosition
    {
        get
        {
            if (type == MovingType.FromCurrentPosition)
            {
                return startPosition + offsetPosition;
            }
            else 
            {
                return destinatePosition;
            }
        }
    }
    [SerializeField] private float moveSpeed = 2f;
    private Vector3 originalPosition;

    private void Start()
    {
        startPosition = transform.position;
        originalPosition = transform.position;
        StartCoroutine(MoveLoopRoutine());
    }

    private IEnumerator MoveLoopRoutine()
    {
        while (true)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, originalPosition,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    
    public override void ApplyEffect(Player player)
    {
        // Moving effect doesn't need player interaction, it moves continuously
    }

    public void OnDrawGizmos()
    {
        Vector3 center = Vector3.zero;
        if (type == MovingType.FromCurrentPosition)
        {
            center =  transform.position + offsetPosition;
        }
        else 
        {
            center = destinatePosition;
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 2);
    }
}
