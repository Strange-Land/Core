using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Networking
{
    [System.Serializable]
    public class ParticipantOrderMapping
    {
        public Dictionary<ulong, ParticipantOrder> _clientToOrder = new Dictionary<ulong, ParticipantOrder>();
        public Dictionary<ParticipantOrder, ulong> _orderToClient = new Dictionary<ParticipantOrder, ulong>();
    
        public ParticipantOrder GetPO(ulong id)
        {
            if (_clientToOrder.ContainsKey(id))
            {
                return _clientToOrder[id];
            }
            Debug.LogError($"No ParticipantOrder found for id {id}");
            return ParticipantOrder.None;
        }
    
        public ulong GetClientID(ParticipantOrder po)
        {
            if (_orderToClient.ContainsKey(po))
            {
                return _orderToClient[po];
            }
            return 0;
        }
    
        public bool AddParticipant(ParticipantOrder po, ulong id)
        {
            if (!_orderToClient.ContainsKey(po))
            {
                _orderToClient.Add(po, id);
                _clientToOrder.Add(id, po);
                return true;
            }
            return false;
        }
    
        public bool RemoveParticipant(ParticipantOrder po)
        {
            if (_orderToClient.ContainsKey(po))
            {
                var id = _orderToClient[po];
                _orderToClient.Remove(po);
                _clientToOrder.Remove(id);
                return true;
            }
            return false;
        }
    
        public ParticipantOrder[] GetAllConnectedPOs()
        {
            return _orderToClient.Keys.ToArray();
        }
    }
}
