using Path = System.IO.Path;

namespace Uma
{
    public enum UmaFileType
    {
        _3d_cutt,
        announce,
        atlas,
        bg,
        chara,
        font,
        fontresources,
        gacha,
        gachaselect,
        guide,
        heroes,
        home,
        imageeffect,
        item,
        lipsync,
        live,
        loginbonus,
        manifest,
        manifest2,
        manifest3,
        mapevent,
        master,
        minigame,
        mob,
        movie,
        outgame,
        paddock,
        race,
        shader,
        single,
        sound,
        story,
        storyevent,
        supportcard,
        uianimation,
        transferevent,
        teambuilding,
        challengematch,
        collectevent,
        ratingrace,
        jobs,
        manualdownloadatlas,
    }
    public class CharaData
    {
        public int Id;

        public int BirthYear;
        public int BirthMonth;
        public int BirthDay;
        public int LastYear;
        public int Sex;

        public int Height;
        public int Bust;
        public int Scale;
        public int Skin;
        public int Shape;
        public int Socks;
        public int TailModelId;
    }
    public class DressData
    {
        public int Id { get; set; }
        public int ConditionType { get; set; }
        public int HaveMini { get; set; }
        public int GeneralPurpose { get; set; }
        public int CostumeType { get; set; }
        public int CharaId { get; set; }
        public int UseGender { get; set; }
        public int BodyShape { get; set; }
        public int BodyType { get; set; }
        public int BodyTypeSub { get; set; }
        public int BodySetting { get; set; }
        public int UseRace { get; set; }
        public int UseLive { get; set; }
        public int UseLiveTheater { get; set; }
        public int UseHome { get; set; }
        public int UseDressChange { get; set; }
        public int IsWet { get; set; }
        public int IsDirt { get; set; }
        public int HeadSubId { get; set; }
        public int UseSeason { get; set; }
        public string DressColorMain { get; set; } = string.Empty;
        public string DressColorSub { get; set; } = string.Empty;
        public int ColorNum { get; set; }
        public int DispOrder { get; set; }
        public int TailModelId { get; set; }
        public int TailModelSubId { get; set; }
        public int MiniMayuShaderType { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
    }
    public class CharaMotionSet
    {
        public int Id;
        public string BodyMotion;
        public int BodyMotionType;
        public int BodyMotionPathSegment;
        public int BodyMotionPlayType;
        public string FaceType;
        public int Cheek;
        public int EyeDefault;
        public string EarMotion;
        public string TailMotion;
        public int TailMotionType;

        public string GetAnimPath()
        {
            string path = $"3d/motion/event/body/type00/anm_eve_type00_{BodyMotion.Replace("_mirror","")}_loop";

            if (BodyMotion.Contains("_mirror"))
            {
                path += "_mirror";
            }

            return path;
        }
    }
    public class UmaDatabaseEntry
    {
        public UmaFileType Type;
        public string Name;
        public string Url;
        public string Checksum;
        public string Prerequisites;
        public long Key;
        private byte[] fKey;
        public bool IsEncrypted => Key != 0;

        public byte[] FKey
        {
            get
            {
                if (fKey == null && IsEncrypted)
                {
                    var baseKeys = Utility.HexStringToBytes(UmaDatabase.ABKey);
                    int baseLen = baseKeys.Length;

                    var keys = new byte[baseLen * 8];

                    byte[] keyBytes = BitConverter.GetBytes(Key);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(keyBytes);
                    }
                    for (int i = 0; i < baseLen; ++i)
                    {
                        byte b = baseKeys[i];
                        int baseOffset = i << 3; // i * 8
                        for (int j = 0; j < 8; ++j)
                        {
                            keys[baseOffset + j] = (byte)(b ^ keyBytes[j]);
                        }
                    }

                    fKey = keys;
                }
                return fKey;
            }
        }

        public string QueryPath()
        {
            return Path.Combine(UmaDatabase.persistentPath.Replace("\\", "/"), $"dat/{Url.Substring(0, 2)}/{Url}");
        }
    }

    /*[System.Serializable]
    public class EmotionKey
    {
        public FacialMorph morph;
        public float weight;
    }

    [System.Serializable]
    public class FaceTypeData
    {
        public string label, eyebrow_l, eyebrow_r, eye_l, eye_r, mouth, inverce_face_type;
        public int mouth_shape_type, set_face_group;
        public FaceEmotionKeyTarget target;
        public List<EmotionKey> emotionKeys;
        public float weight;
    }*/
}
