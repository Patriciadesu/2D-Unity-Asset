using System;
using UnityEngine;
using UnityEngine.Events;

public class FSMPlayer : Singleton<FSMPlayer>
{
    public PlayerState CurrentState;

    public UnityEvent OnTakeDamage;
    public UnityEvent OnDie;
    

    private void Update()
    {
        if (CurrentState != null)
        {
            CurrentState.Update();
        }
    }
}