using UnityEngine;

public class Player : Entity
{
    private void Awake()
    {
        // Automatically set the tag and team ID
        gameObject.tag = "Player";
        teamID = 0;
    }
}
