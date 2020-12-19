using UnityEngine;
using System.Collections;

public class CharacterLocomotion
{
	private Animator	animator;	

	// Use this for initialization
	public CharacterLocomotion(Animator animator)
	{
		this.animator = animator;
	}
	
	public void DoAnimState(int stateNameHash, bool useCrossFade = false, float fadeTime = 0.15f)
	{
		if (useCrossFade)
		{
			animator.CrossFade(stateNameHash, fadeTime);
		}
		else
		{
			animator.Play(stateNameHash, 0, 0.0f);
		}
	}
}