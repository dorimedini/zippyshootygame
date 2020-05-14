/**
 * < V > Stuff I already did
 * < V > Connect network layer, play with it active without terrible performance
 * ------- V > Create room if can't random join, or join random room
 * ------- V > On join, instantiate player on random pentagon
 * ------- V > Disable main camera on join
 * ------- V > Disable physics, camera, audiolistener on player prefab, enable on self join room
 * ------- V > Find the equivalent of a PhotonView, attach to player
 * ------- V > NetworkCharacter should do a 0.1f Lerp from current position/rotation to actual position/rotation while moving
 * < V > Add player model and animation (ready-made, temporary)
 * ------- V > TBD: massage mocap animations to use the Kylebot?
 * ------- V > Get walk animation working perfectly
 * ------- V > Add option to run
 * ------- V > Run, transitions, walk/run left/right
 * ------- V > Jump (no roll?)
 * ------- V > Fall, when in air after jump sequence or when falling without jumping first (launch, fall off cliff)
 * < V > Re-implement projectile firing on network
 * < V > Implement pillar extend / retract on network (on projectile hit)
 * ------- V > Test: Self launch, launch another player
 * < V > Add projectile damage
 * < V > Successfully play with 2 people concurrently, have one take damage from the other
 * < V > Smaller player collider
 * < V > Press and hold for more projectile launch power (add dummy GUI indication until weapong animation kicks in)
 * < V > Fullscreen! (THIS IS JUST AN OPTION IN BUILD-->PLAYER SETTINGS)
 * < V > Game parameters: allow users (testers) to control gravity, jump force, look speed, projectile speed, launch height, projectile blast radius
 * < V > Health indication
 * < V > Projectile explosion effect - blast radius, explosion force
 * < V > Die, respawn
 * < V > Fix IsGrounded to use RaycastAll so we don't get annoying errors for finding a projectile / other player under us
 * < V > Movement mechanic - grapple to floor (simple raycast and acceleration until either a. speed goes down (collision) or b. we get close enough)
 * < V > Explosions should damage the enemy players in the radius, proportional to distance
 * < V > Projectiles: only the firing player puts a collider on the projectile and notifies all other players about collisions. Other players only instantiate
 *       local graphic representations, and destroy the projectile when informed by the firer.
 *       This is a good compromise, because each player sees his own projectile the most, and this ensures instantiation and collision gives immediate feedback to 
 *       the local player.
 * < V > Scrollable settings
 * < V > Message box (for game alerts, and later for chat)
 * < V > Show respawn timer while waiting
 * < V > Maybe make sphere radius a user-parameter? Then we could make grapple distance dependent on radius
 * < V > Make weapon-charge an optional mechanic
 * < V > Sounds: walk, fire, hit, launch (only self hears)
 * < V > Pillar hit effect
 * < V > Take camera back a bit so we don't see through walls
 * < V > Grappling twice in a row jerks the camera; lerp it (it looks like FOV isn't lerping)
 * < V > Visible delay in player knockback on explosion. Maybe disable network-character input for half a second and fling the enemy locally, lerp later?
 * < V > Ragdoll doesn't get the explosion knockback if the hit kills a player. Fix it
 * < V > Respawn camera should use dynamic zoom, in case the player dies an a high pillar / the body falls off a high pillar
 * < V > Projectile should take the current player speed into account when computing initial speed
 * < V > Grapple superman animation should be fixed-oriented towards target point.
 * < V > Destroy hookshot on death! Both local and network
 * < V > Hide mouse cursor
 * < V > Expose EHN as parameter
 * < V > Pause menu, better settings, option to quit
 * <   > Hard to see other players. Make the "find the line of sight" game fun!
 *   X      Larger models?                                          (mark)              --> I think just setting smaller radius does the trick
 *   V      Trail of smoke behind projectiles?                      (mark)
 *   V      Activate sunray blasts to random player location        (keep moving, mark)
 *   V      Hanzo-vision powerup (fired from the sun, pending test) (clue)
 *   V      Sun-pointers powerup (color arrows toward each player)  (clue)
 *   V      Heat-seeking missiles with lock-on time?                (reward)
 *          Powerup mechanic idea: orbit around the sun
 *          Powerup mechanic idea: 3 collectables, have X seconds to collect all of them (from the moment the first was collected)
 *          Powerup mechanic idea: the sun advertises where it's gonna fall, but it's going to land on a pillar face
 * <   > Weapon cooldown doesn't seem to be in effect for lock-on mode
 * <   > Don't start by playing a sound at target player when targeting starts, if the enemy is single-click firing it's annoying
 * <   > During grapple, bring the camera to where the head is (rotation should be handled automatically by mouselook)
 * <   > Sunshoe mode: press a button to reverse gravity and walk on the sun
 * <   > Max height of pillar isn't working, small radius --> hits the sun
 * <   > Smaller projectile collider, so it passes through tight spaces
 * <   > Wall grab? When close to a wall and not on the ground, grab a wall while jump is pressed
 * <   > Camera still falls through pillar mesh
 * <   > Who died, who killed who
 * <   > Speculative explosion force looks bad. Try without it?
 * <   > Game modes:
 *          Respawn mode
 *          U ded u ded mode (kill cam?)
 *          Initial pillar heights (doesn't have to be all at zero at the beginning)
 * <   > Leaderboard, stats
 * <   > Gravity gun: player points and shoots, that's where gravity is... for...?
 * <   > Really need ambient music...
 * <   > Still a bit jerky when exploding a remote player and the remote starts sending the updates. Maybe use an even system, on ACK from remote player re-activate motion sync?
 * <   > Pentagon hit should expand pentagon to the max, and any hex in an EHN-depth BFS from the pentagon should expand by min(maxheight, height+maxheight/(deg+1))
 * <   > Landing feels soft... maybe a little camera bounce?
 * <   > Fix GUI jitter... need to do some research / find someone experienced
 * <   > GUI: Waiting room to join game (will be useful for respawn later)
 * < V > Hitting the sun should 'charge' it until a maximal value on which a 'discharge' occurs damaging proportional to proximity
 * <   > Weapon model and animation (maybe make in blender? Or take ready made)
 * -------   > This is a good point to add arms to self-player
 * -------   > Maybe don't launch projectile from directly in front of face
 * <   > IMPORTANT: Make the grapple feel really good! It's faster than walking so it's probably going to be the main movement mechanic
 * <   > Better projectile animation (how to launch with precision without shooting from the camera?)
 *?<   > Better projectile model...?
 * <   > Texture the projectile
 * <   > Get non-temporary player models / animations
 * -------   > Add roll on land?
 * <   > Non-temporary weapon models
 * <   > Check the isMasterClient thing in Photon, maybe makes the projectile management better?
 * <   > Moon: make some hard-to-hit, syncronized, random-orbit "moon" around the sun; players get rewarded / punished for hitting it directly, depending on
 *       current health maybe..?
 * <   > Research: can player's ping values be used to implement good speculative syncing?
 * <   > Make new list
 */
