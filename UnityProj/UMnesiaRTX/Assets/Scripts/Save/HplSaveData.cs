using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Save
{
    // The save file (cLuxSaveGame_SaveData and the save data classes of LuxSavedGame*.h / LuxSavedEngineTypes.h) as
    // JsonUtility classes: lists of plain classes, no dictionaries or tuples. The map cache of visited maps uses the
    // same classes in memory.

    /// <summary>cLuxSaveGame_SaveData: everything a save file holds.</summary>
    [Serializable]
    public sealed class SaveGame
    {
        public const int CurrentVersion = 1;
        public string game = "RTXMnesia";
        public int version = CurrentVersion;
        public string campaign = "main";
        public string date = "";
        public bool hardMode;
        /// <summary>msMapFolder + cLuxSavedMap::msFileName: the map file relative to the game folder.</summary>
        public string mapFile = "";
        public SavedMap map = new();
        public List<SavedMap> savedMaps = new();
        public PlayerSave player = new();
        public InventorySave inventory = new();
        public JournalSave journal = new();
        public MusicSave music = new();
        public EffectsSave effects = new();
        public List<SavedVar> globalVars = new();
        public List<string> hintsGiven = new(), hintsBlocked = new();
        public InsanitySave insanity = new();
        public LoadScreenSave loadScreen = new();
    }

    // ================================================================== maps

    [Serializable] public sealed class SavedVar { public string name, value; }

    [Serializable]
    public sealed class SavedTimer
    {
        public string name, function;
        public float count;
    }

    [Serializable]
    public sealed class SavedUseItem
    {
        public string name, item, entity, function;
        public bool autoDestroy;
    }

    /// <summary>cLuxCollideCallback_SaveData.</summary>
    [Serializable]
    public sealed class SavedCollideCallback
    {
        public string entity, function;
        public bool delete;
        public int states;
    }

    /// <summary>cLuxEntityConnection_SaveData.</summary>
    [Serializable]
    public sealed class SavedConnection
    {
        public string name, entity, callback;
        public bool invert;
        public int states;
    }

    /// <summary>iLuxInteractConnection_SaveData (Rope: the rope's name and motor speeds; MoveObject: its name).</summary>
    [Serializable]
    public sealed class SavedInteractConnection
    {
        public string type, name, target;
        public bool interactionOnly, invert;
        public int statesUsed;
        public float speedMul, minSpeed, maxSpeed;
    }

    /// <summary>cLuxSavedGameEntity: an entity's collide callbacks, connections and a prop's interact connections, by entity key.</summary>
    [Serializable]
    public sealed class SavedEntityLinks
    {
        public string key;
        public List<SavedCollideCallback> collideCallbacks = new();
        public List<SavedConnection> connections = new();
        public List<SavedInteractConnection> interactConnections = new();
    }

    /// <summary>cEngineVerletParticle_SaveData (HPL2 space).</summary>
    [Serializable]
    public sealed class SavedRopeParticle
    {
        public Vector3 position, prevPosition, smoothPosition;
        public float invMass;
    }

    /// <summary>cEngineRope_SaveData: the physics rope of a rope entity.</summary>
    [Serializable]
    public sealed class SavedRope
    {
        public bool created;
        public float totalLength, minTotalLength, maxTotalLength, segmentLength, damping, strength, stiffness;
        public int maxIterations;
        public bool motorActive, autoMove;
        public float motorWantedLength, motorSpeedMul, motorMinSpeed, motorMaxSpeed, autoMoveSpeed, autoMoveAcc, autoMoveMaxSpeed;
        public string startBody = "", endBody = "";
        public Vector3 startBodyLocalPos, endBodyLocalPos;
        public List<SavedRopeParticle> particles = new();
    }

    /// <summary>cLuxArea_Sticky_SaveData: the attached prop body (by names) and its saved mass / gravity.</summary>
    [Serializable]
    public sealed class SavedSticky
    {
        public string attachedEntity = "", attachedBody = "";
        public float attachedBodyMass, setMtxTime;
        public bool attachedBodyGravity, attachedEntityFullGameSaved;
    }

    /// <summary>cLuxSavedGameEntityFunc: a look / interact / callback / state change function by entity key.</summary>
    [Serializable]
    public sealed class SavedEntityFunc
    {
        public string key, function;
        public bool autoRemove;
    }

    [Serializable]
    public sealed class SavedBody
    {
        public Vector3 position, linearVelocity, angularVelocity;
        public Quaternion rotation;
    }

    [Serializable]
    public sealed class SavedJoint
    {
        public float minAngle, maxAngle;
        public bool broken;
    }

    [Serializable]
    public sealed class SavedPatrolNode
    {
        public int nodeId;
        public float waitTime;
        public string animation;
        public bool loopAnimation;
    }

    /// <summary>cLuxSavedGameEnemy (map cache): the patrol of an enemy that comes back inactive.</summary>
    [Serializable]
    public sealed class SavedEnemyPatrol
    {
        public string key;
        public List<SavedPatrolNode> nodes = new();
    }

    /// <summary>iLuxEnemy_SaveData: the enemy's own fields, its body, patrol, animation, mover and path.</summary>
    [Serializable]
    public sealed class EnemySave
    {
        public Vector3 feet;
        public float yaw;
        public int soundState;
        public List<SavedField> mover = new();
        public List<SavedPatrolNode> patrol = new();
        public string animation = "";
        public float animationTime;
        public bool animationLoop;
        public bool pathMoving;
        public Vector3 pathGoal;
        public List<int> pathNodes = new();
    }

    /// <summary>iLuxEntity_SaveData of a whole entity (FullGameSave in the map cache, every entity of the current map).</summary>
    [Serializable]
    public sealed class SavedFullEntity
    {
        public string key, name, file;
        public bool active, interactionDisabled, effectsActive, staticPhysics, lit;
        public float maxFocusDistance;
        public string callbackFunc, stateChangeCallback, interactCallback, lookAtCallback;
        public bool interactRemove, lookAtRemove;
        public Vector3 position;
        public Quaternion rotation;
        public SavedEntityLinks links = new();
        public List<SavedBody> bodies = new();
        public List<SavedJoint> joints = new();
        public List<SavedField> fields = new();
        public bool isEnemy;
        public EnemySave enemy = new();
        public SavedRope rope = new();
        public SavedSticky sticky = new();
        public List<SavedAnimationState> animations = new();
    }

    /// <summary>cEngineAnimationState_SaveData: a mesh animation state of a prop (cEngineMeshEntity_SaveData::mvAnimations).</summary>
    [Serializable]
    public sealed class SavedAnimationState
    {
        public bool active, loop;
        public float weight, fadeStep, timePosition, speed;
    }

    /// <summary>cEngineSound_SaveData.</summary>
    [Serializable]
    public sealed class SavedSound
    {
        public string name, file;
        public bool placed, active, stopped, removeWhenOver;
        public float minDistance, maxDistance, volume;
        public Vector3 position;
    }

    /// <summary>cEnginePS_SaveData.</summary>
    [Serializable]
    public sealed class SavedParticleSystem
    {
        public string name, file;
        public bool placed, active, visible, fadeAtDistance;
        public Matrix4x4 localMatrix;
        public Color color;
        public float minFadeStart, minFadeEnd, maxFadeStart, maxFadeEnd;
        public bool[] emittersActive = Array.Empty<bool>();
    }

    [Serializable]
    public sealed class SavedLight
    {
        public string name;
        public World.AmnesiaLight.SaveData data = new();
    }

    /// <summary>
    /// cLuxSavedGameMap (the cache of a visited map) and cLuxSavedMap (the current map of a save, <see cref="full"/>:
    /// every entity whole, the check point and the lantern callback too).
    /// </summary>
    [Serializable]
    public sealed class SavedMap
    {
        public string name = "", displayNameEntry = "";
        public bool full;
        public int numberOfQuests;
        public string lanternLitCallback = "";
        public bool fogActive;
        public Color fogColor;
        public float fogStart, fogEnd, fogFalloffExp;
        public string checkPointName = "", checkPointStartPos = "", checkPointCallback = "", checkPointMusic = "";
        public int checkPointCount, checkPointMusicPrio;
        public bool checkPointMusicResume;
        public float checkPointMusicVolume;
        public List<SavedVar> vars = new();
        public List<SavedUseItem> useItemCallbacks = new();
        public List<SavedTimer> timers = new();
        public List<SavedCollideCallback> playerCollideCallbacks = new();
        public List<SavedLight> lights = new();
        public List<SavedSound> sounds = new();
        public List<SavedParticleSystem> particleSystems = new();
        public List<SavedFullEntity> fullEntities = new();
        public List<SavedEntityLinks> links = new();
        public List<SavedEntityFunc> look = new(), interact = new(), callback = new(), stateChange = new();
        public List<string> disabled = new(), lockedDoors = new(), lockedLevelDoors = new(), activeItems = new(),
            unbrokenContainers = new(), brokenContainersWithActiveItem = new(), unlitLamps = new();
        public List<SavedEnemyPatrol> enemies = new();
    }

    // ================================================================== game modules

    /// <summary>cLuxPlayer_SaveData: what carries over between maps plus the body, the view and the script effects on the camera.</summary>
    [Serializable]
    public sealed class PlayerSave
    {
        public Player.AmnesiaPlayer.Carry carry = new();
        public Vector3 feet;
        public float yaw, pitch;
        public bool active = true;
        public int coins;
        public List<SavedField> fields = new();
    }

    /// <summary>cLuxInventory_SaveData.</summary>
    [Serializable]
    public sealed class InventorySave
    {
        public List<ScriptApi.HplGame.Item> items = new();
        public List<ScriptApi.HplGame.CombineCallback> combineCallbacks = new();
        public bool scriptStarted, disabled;
    }

    /// <summary>cLuxJournal_SaveData.</summary>
    [Serializable]
    public sealed class JournalSave
    {
        public List<ScriptApi.HplGame.JournalEntry> entries = new();
        public List<ScriptApi.HplGame.QuestNote> quests = new();
        public int lastReadEntry = -1;
        public bool showJournalOnDiaryPickup = true;
    }

    [Serializable]
    public sealed class MusicSlotSave
    {
        public string file = "";
        public bool loop, resume;
        public float volume;
    }

    /// <summary>cLuxMusicHandler_SaveData.</summary>
    [Serializable]
    public sealed class MusicSave
    {
        public string currentMusic = "";
        public float currentVolume, currentMaxVolume, currentVolumeAdd, currentTime;
        public bool currentLoop;
        public int currentMaxPrio = -1;
        public bool enemyClosePlaying, attackPlaying, searchPlaying;
        public List<MusicSlotSave> music = new();
    }

    /// <summary>cLuxVoiceData.</summary>
    [Serializable]
    public sealed class VoiceSave
    {
        public string voiceFile, effectFile, text;
        public bool usePosition;
        public Vector3 position;
        public float minDistance, maxDistance;
    }

    /// <summary>cLuxEffectHandler_SoundMul_SaveData.</summary>
    [Serializable]
    public sealed class SoundMulSave
    {
        public bool active;
        public float val = 1f, dest = 1f, speed;
        public int types;
        public bool destroyAtDest;
    }

    /// <summary>cLuxEffectHandler_SaveData.</summary>
    [Serializable]
    public sealed class EffectsSave
    {
        public List<SavedField> fade = new(), flash = new(), imageTrail = new(), sepia = new(), radialBlur = new();
        public bool voiceActive;
        public string voiceOverCallback = "";
        public List<VoiceSave> voices = new();
        public List<SoundMulSave> globalVolume = new(), globalSpeed = new();
    }

    /// <summary>cLuxInsanityHandler_SaveData.</summary>
    [Serializable]
    public sealed class InsanitySave
    {
        public float newEventCount;
        public List<bool> used = new();
        public List<string> disabledSets = new();
    }

    /// <summary>cLuxLoadScreenHandler_SaveData.</summary>
    [Serializable]
    public sealed class LoadScreenSave
    {
        public string textCat = "", textEntry = "", textImage = "";
        public int textRandomNum, prevTextNumBitFlags;
    }
}
