using UnityEngine;

public class FixColliderContactDistanceOnStart : MonoBehaviour
{
    private void Start() {
        FixBoxCollider();

        // TODO: Fix for different kind of colliders
    }

    private void FixBoxCollider() {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.size = new Vector2(CalculateContactDifference(col.size.x, transform.lossyScale.x), CalculateContactDifference(col.size.y, transform.lossyScale.y));

        float CalculateContactDifference(float colliderSize, float objectScale) {
            float totalSize = colliderSize * objectScale;
            float correctedSize = totalSize - Physics2D.defaultContactOffset * 4;
            
            if(correctedSize < Physics2D.defaultContactOffset) 
                correctedSize = Physics2D.defaultContactOffset;// * 0.3f;

            return correctedSize / totalSize * colliderSize;
        }
    }
}
