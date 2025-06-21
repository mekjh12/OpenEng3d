using System.Collections.Generic;

namespace Physics.Collision
{
    public class CollisionData
    {
        float _friction = 0.0f;
        float _restitution = 0.3f;
        List<Contact> _contacts;
        uint _contactLeft;
        uint _contactMaxCount = 0;

        public List<Contact> Contacts
        {
            get => _contacts;
        }

        /// <summary>
        /// 마찰력
        /// </summary>
        public float Friction
        {
            get => _friction;
            set => _friction = value;
        }

        /// <summary>
        /// 반발계수
        /// </summary>
        public float Restitution
        { 
            get => _restitution;
            set => _restitution = value;
        }

        /// <summary>
        /// 충돌데이터의 저장할 수 있는 데이터의 수
        /// </summary>
        public uint ContactLeft
        {
            get => _contactLeft;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="contactMaxCount"></param>
        public CollisionData(uint contactMaxCount)
        {
            _contacts = new List<Contact>();
            _contactLeft = contactMaxCount;
            _contactMaxCount = (uint)contactMaxCount;
        }

        /// <summary>
        /// 접촉을 추가한다.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public bool AddContact(Contact contact)
        {
            if (_contactLeft > 0)
            {
                _contacts.Add(contact);
                _contactLeft--;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            if (_contacts != null)
            {
                _contacts.Clear();
                _contactLeft = _contactMaxCount;
            }
        }
    }
}
