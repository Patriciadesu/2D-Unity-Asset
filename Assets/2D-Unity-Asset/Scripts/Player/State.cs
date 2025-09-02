using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerState
{
    private FSMPlayer _player;
    private Animator _animator;
    public PlayerState()
    {
        _player = FSMPlayer.Instance;
        _animator = _player.GetComponent<Animator>();
    }
    public virtual void Enter()
    {
        _player.CurrentState = this;
    }
    public virtual void Exit()
    {

    }
    public virtual void Update()
    {

    }
}