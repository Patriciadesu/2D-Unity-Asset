using UnityEngine;

public class PlayerExtension : MonoBehaviour
{
    protected Player _player;
    public virtual void OnStart(Player player)
    {
        _player = player;
    }

}






