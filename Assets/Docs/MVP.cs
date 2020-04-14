/**
 * Zippy Spherey Shooty Game
 * 
 * 
 * -----------
 * THE SETTING
 * -----------
 * FPS, Multiplayer Arena type game, in which several players are placed inside of a sphere
 * tiled by (mostly) hexagons. Gravity pushes away from the center point of the sphere (the
 * ORIGIN), so the players walk around on the tiles on the inside of the sphere.
 * 
 * Each tile can be EXPANDED to have a HEIGHT of X, and can be RETRACTED to some HEIGHT X.
 * This extends the inner face of the tile towards ORIGIN by X (X is capped at the RADIUS 
 * of the sphere, but MAX_HEIGHT will be the real cap), or retracts the face back towards 
 * the sphere's bounderies.
 * 
 * If a tile's HEIGHT is reduced bellow MIN_HEIGHT, the tile becomes DESTROYED and is 
 * removed. Players can fall through DESTROYED tiles.
 * 
 * All tiles begin UNLOCKED, and at height INITIAL_HEIGHT.
 * 
 * All UNLOCKED tiles that have not been EXPANDED or RETRACTED for TILE_DECAY seconds, start
 * to grow/shrink back to INITIAL_HEIGHT.
 * 
 * 
 * ---------
 * MECHANICS
 * ---------
 * Movement and camera mechanics - this is an FPS. As for interaction (weapons), players have 
 * a RED GUN, a BLUE GUN and a way to GRAPPLE:
 * - RED GUN is destructive
 * - BLUE GUN is constructive
 * - The GRAPPLE can grab edges of the environment and launch the player in that direction.
 * 
 * By default, the behavior of RED and BLUE is as follows:
 * 
 * - When the player fires the RED GUN a ballistic projectile is fired in the direction of the
 *   crosshair and activates upon impact with a hexagon H:
 *   
 *   > If an enemy player P is on H, P takes damage.
 *   
 *   > If H is UNLOCKED and EXPANDED to some X where X >= MIN_HEIGHT, then H is RETRACTED
 *     by some factor TBD.
 *     
 *     If the retraction would put H at height below MIN_HEIGHT, set the height to MIN_HEIGHT.
 *     
 *     The retraction should be bullet-animated (initial burst of speed, rapid decay).
 *     
 *   > If H is at height MIN_HEIGHT, H becomes DESTROYED.
 *   
 * - When the player fires the BLUE GUN a ballistic projectile is fired in the direction of the
 *   crosshair and activates upon impact with either a player P or a hexagon H:
 *   
 *   > If the height of H is less than MAX_HEIGHT, H becomes EXPANDED by some value TBD, capped
 *     at MAX_HEIGHT.
 *     
 *     This expansion shoots off fast enough to LAUNCH any player (ally or enemy)
 *     into the air. This should only apply extra velocity / acceleration in the direction of
 *     ORIGIN, so the customer's/victim's original speed is taken into account. This way, unless
 *     the victim is standing still, the victim won't land back on H.
 *     
 *     (this should be tweaked s.t. players are able to use this on themselves by running in the 
 *     direction they want to launch and shooting in front of them).
 *     
 *   > If H is at MAX_HEIGHT and is UNLOCKED, H becomes LOCKED.
 *     
 *     This mechanic allows (and hopefully - causes) players to eventually shrink the playable area
 *     into a sphere of radius (RADIUS - MAX_HEIGHT).
 * 
 * 
 * -------------
 * FUTURE DREAMS
 * -------------
 * 
 * EVENTS
 * - The sun periodically sends a solar flare, maximizing tiles in an arc?
 * - Shooting the sun:
 *   . Charge it? The one who fires the last (overcharging) hit on the sun causes a RAIN OF FIRE, doing damage and expanding
 *     a random X percent of tiles
 * 
 * WEAPON MODS
 * Explore other constructive/destructive ways to interact with this arena. Some ideas:
 * 
 * - BOTH GUNS
 * 
 *   > Explosive ammo. Doesn't EXPAND or RETRACT by much and BLUE doesn't launch very high, but covers
 *     more ground. Possible use case: imagine a game mode where INITIAL_HEIGHT is set to MIN_HEIGHT.
 *     A player can equip a BLUE GUN with explosive ammo and self-target to slightely increase the
 *     height of all tiles around the player, so the enemy can't one-shot destroy any of them.
 * 
 *   > Shapes! Fire a line of projectiles (BLUE will create a wall and RED will hopefully destroy tiles
 *     in a line), closed cycles for protection / encagement... etc
 *   
 *   > Charge fire: Aim steady for CHARGE_DELAY seconds to launch a CHARGED projectile. Two consecutive 
 *     charged hits (within CHARGE_HIT_WINDOW seconds) destroy (RED) or fully expand and lock (BLUE) 
 *     tiles.
 *
 * 
 * - SPECIAL ROUNDS (rounds as in those things that are fired from guns)
 * 
 *   > BLUE GUN: BRIDGE round. Expands tiles in such a way that the player can walk to the tile H hit
 *     by the projectile, without curvature:
 *     
 *                                        O(rigin)
 *     -__                                                                         __-
 *        --P____________________________new_walkway____________________________H--
 *                -----______                                  ________-----
 *                           -------------________-------------
 *                           
 *     This can be used to help allies in a different area of the sphere by raising a wall between them
 *     and enemies, or providing escape routes.
 *     
 *     Maybe it would be a good idea to expand the tiles around the walkway a bit more than the walkway
 *     tiles, to provide cover for those who cross.
 *     
 *     This type of round shouldn't launch players in the air.
 */
