using RTXMnesia.Dynamics;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxCommentaryIcon (LuxCommentaryIcon.cpp): a developer commentary icon, off unless the commentary option is
    /// on (the map turns the icons on and off, see HplMap.UpdateCheckCommentaryIconActive). Its .comment file
    /// (instance variable File) gives the talker, topic and sound; the focus text is "Talker\n\"Topic\"",
    /// interaction starts or stops the commentary (cLuxEffect_PlayCommentary). While it plays, the RotateSubMesh
    /// (the cog) turns 2.5 rad/s about its Y axis and turns on to a half turn when it stops; a particle system
    /// (ps_commentary_icon.ps) marks it.
    /// </summary>
    public sealed class PropCommentaryIcon : HplProp
    {
        string _rotateSubMesh = "", _talker = "", _topic = "", _soundFile = "";
        float _rotAngle;
        Transform _rotated;
        Quaternion _rotatedLocal;

        public bool PlayingSound { get; private set; }
        public string Talker => _talker;
        public string Topic => _topic;
        public string SoundFile => _soundFile;

        public PropCommentaryIcon(AmnesiaEntity entity) : base(entity, HplEntityType.CommentaryIcon) { }

        protected override string DefaultFocusDistanceKey => "Commentary_MaxFocusDist";

        protected override void LoadVariables()
        {
            _rotateSubMesh = VarString("RotateSubMesh", "");
            // cLuxCommentaryIconLoader: the focus distance is always the default one.
            MaxFocusDistance = ConfigFloat(DefaultFocusDistanceKey);
        }

        protected override void LoadInstanceVariables()
        {
            string file = InstanceString("File", "");
            var data = HplCommentaryAssets.Instance != null ? HplCommentaryAssets.Instance.Find(file) : null;
            if (data == null)
            {
                HplLog.Error($"Could not load flashback file: '{System.IO.Path.ChangeExtension(file, "comment")}'");
                return;
            }
            _talker = data.talker;
            _topic = data.topic;
            _soundFile = data.soundFile;
        }

        protected override void OnSetupAfterLoad()
        {
            if (_rotateSubMesh == "") return;
            foreach (var t in GameObject.GetComponentsInChildren<Transform>(true))
                if (t.name == _rotateSubMesh && t.GetComponent<MeshRenderer>() != null)
                {
                    _rotated = t;
                    _rotatedLocal = t.localRotation;
                    break;
                }
        }

        public override bool CanInteract(HplPhysicsBody body) => true;

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            var effect = Map.effects.PlayCommentary;
            if (PlayingSound) effect.Stop();
            else effect.Start(_talker, _topic, _soundFile, Id);
            SetPlayingSound(!PlayingSound);
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) => FocusCrosshair.Default;

        public override string GetFocusText() => _talker + "\n\"" + _topic + "\"";

        public void SetPlayingSound(bool playing)
        {
            if (PlayingSound == playing) return;
            PlayingSound = playing;
            var particles = Map.ParticleWorld;
            if (particles == null) return;
            if (PlayingSound)
            {
                var ps = particles.Create("_commentary_icon_ps", "ps_commentary_icon.ps");
                if (ps != null)
                {
                    Vector3 pos = PhysicsBodies.Count > 0 ? PhysicsBodies[0].Position : GameObject.transform.position;
                    ps.LocalMatrix = Matrix4x4.Translate(pos);
                    ps.IsSaved = false;
                }
            }
            else
            {
                var ps = particles.Systems.Find(s => s.Name == "_commentary_icon_ps");
                if (ps != null) ps.Kill();
            }
        }

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (PlayingSound) _rotAngle += timeStep * 2.5f;
            else if (_rotAngle != 0f)
            {
                _rotAngle = Hpl.HplMatrix.Wrap(_rotAngle, 0f, Mathf.PI);
                _rotAngle += timeStep * 2.5f;
                if (_rotAngle > Mathf.PI) _rotAngle = 0f;
            }
            // Sub mesh local matrix × RotateXYZ(0, angle, 0); a Y angle of HPL2 is the negative one in Unity's mirrored space.
            if (_rotated != null) _rotated.localRotation = _rotatedLocal * Quaternion.Euler(0f, -_rotAngle * Mathf.Rad2Deg, 0f);
        }
    }
}
