using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Animate
{
    public class ArmatureLinker
    {
        Dictionary<string, string> _aTob;
        Dictionary<Bone, List<Bone>> _bToa;
        Dictionary<Bone, Bone> _nearestAncestorList;
        Armature _a;
        Armature _b;

        public Bone[] DestBones => _bToa.Keys.ToArray();

        public ArmatureLinker()
        {
            _aTob = new Dictionary<string, string>();
            _bToa = new Dictionary<Bone, List<Bone>>();
            _nearestAncestorList = new Dictionary<Bone, Bone>();
        }

        public Bone[] GetBones(Bone bone)
        {
           if (_bToa.ContainsKey(bone))
            {
                return _bToa[bone].ToArray();
            }
            return null;
        }

        private void FindNearestAncestor()
        {
            foreach (Bone bone in _b.RootBone.ToBFSList())
            {
                if (!_bToa.ContainsKey(bone)) continue;

                // 최인접 연계 조상 찾기
                Bone nearestAncestor = bone.Parent;
                while (nearestAncestor != null && !_bToa.ContainsKey(nearestAncestor))
                {
                    nearestAncestor = nearestAncestor.Parent;
                }
                _nearestAncestorList[bone] = nearestAncestor;
            }
        }

        public void LinkRigs(Armature a, Armature b)
        {
            // 뼈대를 보관한다.
            _a = a;
            _b = b;

            // 소스 뼈대를 순회하면서 대상 딕셔너리에 0,1,N 소스 뼈대리스트를 만든다.
            foreach (var item in _aTob) 
            {
                string aName = item.Key;
                string bName = item.Value;

                Bone srcBone = a.DicBones[aName];
                Bone dstBone = b.DicBones[bName];

                if (_bToa.ContainsKey(dstBone))
                {
                    if (!_bToa[dstBone].Contains(srcBone))
                    {
                        _bToa[dstBone].Add(srcBone);
                    }
                }
                else
                {
                    _bToa[dstBone] = new List<Bone>();
                    _bToa[dstBone].Add(srcBone);
                }
            }

            
            FindNearestAncestor();
        }

        public void LoadLinkFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                string[] lines = File.ReadAllLines(fileName);
                foreach (string line in lines)
                {
                    if (line.StartsWith("//")) continue; // Skip comments
                    string[] parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        _aTob[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
        }


    }
}
