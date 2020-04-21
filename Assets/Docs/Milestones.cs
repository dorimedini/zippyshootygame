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
 * <   > Projectile explosion effect
 * <   > Pillar hit effect
 * <   > Player hit effect (GUI included)
 * <   > Better projectile animation (how to launch with precision without shooting from the camera?)
 *?<   > Better projectile model...?
 * <   > Texture the projectile
 * <   > Particle effects for projectile in air and on hit
 * <   > Weapon model and animation (maybe make in blender? Or take ready made)
 * -------   > This is a good point to add arms to self-player
 * <   > Movement mechanic - grapple on pillar edge and launch
 * <   > Get non-temporary player models / animations
 * -------   > Add roll on land?
 * <   > Non-temporary weapon models
 * <   > Pentagon hit should expand pentagon to the max, and any hex in an EHN-depth BFS from the pentagon should expand by min(maxheight, height+maxheight/(deg+1))
 * <   > Add option for 3rd-person camera? Lots of angle issues, should probably follow a tut
 * <   > Make new list
 */
