/**
 * < V > Stuff I already did
 * < V > Connect network layer, play with it active without terrible performance
 * ------- V > Create room if can't random join, or join random room
 * ------- V > On join, instantiate player on random pentagon
 * ------- V > Disable main camera on join
 * ------- V > Disable physics, camera, audiolistener on player prefab, enable on self join room
 * ------- V > Find the equivalent of a PhotonView, attach to player
 * ------- V > NetworkCharacter should do a 0.1f Lerp from current position/rotation to actual position/rotation while moving
 * <   > Add player model and animation (ready-made, temporary)
 * <   > Send tile extend / retract event on network
 * -------   > Implement
 * -------   > Make sure projectiles don't have extra physics other than the syncing (need new ProjectileManager class with RPC calls?)
 * -------   > Test: Self launch, launch another player
 * <   > Add projectile damage
 * <   > Successfully play with 2 people concurrently, have one take damage from the other
 * <   > Weapon model and animation (maybe make in blender? Or take ready made)
 * <   > Movement mechanic - grapple on pillar edge and launch
 * <   > Better projectile animation (how to launch with precision without shooting from the camera?)
 *?<   > Better projectile model...?
 * <   > Texture the projectile
 * <   > Particle effects for projectile in air and on hit
 * <   > Get non-temporary player models
 * <   > Non-temporary weapon models
 * <   > Pentagon hit should exand pentagon to the max, and any hex in an EHN-depth BFS from the pentagon should expand by min(maxheight, height+maxheight/(deg+1))
 * <   > Make new list
 */
