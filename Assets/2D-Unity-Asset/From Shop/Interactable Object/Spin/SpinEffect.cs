using UnityEngine;

public class SpinEffect : ObjectEffect
{
    public enum AxisOption { X, Y, Z, Custom }

    [SerializeField] private AxisOption axis = AxisOption.Y;
    [SerializeField] private Vector3 customAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 90f;

    private Vector3 GetRotationAxis()
    {
        return axis switch
        {
            AxisOption.X => Vector3.right,
            AxisOption.Y => Vector3.up,
            AxisOption.Z => Vector3.forward,
            AxisOption.Custom => customAxis,
            _ => Vector3.up
        };
    }


    private void Update()
    {
        Vector3 rotationAxis = GetRotationAxis();
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}
