using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Networking
{
    [System.Serializable]
    public class ParticipantOrderMapping
    {
        public Dictionary<ulong, ParticipantOrder> _clientToOrder = new Dictionary<ulong, ParticipantOrder>();
        public Dictionary<ParticipantOrder, List<ulong>> _orderToClient = new Dictionary<ParticipantOrder, List<ulong>>();

        public ParticipantOrder GetPO(ulong id)
        {
            if (_clientToOrder.ContainsKey(id))
            {
                return _clientToOrder[id];
            }
            Debug.LogError($"No ParticipantOrder found for id {id}");
            return ParticipantOrder.None;
        }

        public List<ulong> GetClientIDs(ParticipantOrder po)
        {
            if (_orderToClient.ContainsKey(po))
            {
                return _orderToClient[po];
            }
            return new List<ulong>();
        }

        public ulong GetClientID(ParticipantOrder po)
        {
            var clients = GetClientIDs(po);
            return clients.Count > 0 ? clients[0] : 0;
        }

        public bool AddParticipant(ParticipantOrder po, ulong id)
        {
            if (po == ParticipantOrder.Researcher)
            {
                if (!_orderToClient.ContainsKey(po))
                {
                    _orderToClient.Add(po, new List<ulong>());
                }
                _orderToClient[po].Add(id);
                _clientToOrder.Add(id, po);
                return true;
            }
            else
            {
                if (!_orderToClient.ContainsKey(po))
                {
                    _orderToClient.Add(po, new List<ulong>());
                    _orderToClient[po].Add(id);
                    _clientToOrder.Add(id, po);
                    return true;
                }
                return false;
            }
        }

        public bool RemoveParticipant(ulong id)
        {
            if (_clientToOrder.ContainsKey(id))
            {
                var po = _clientToOrder[id];
                _clientToOrder.Remove(id);

                if (_orderToClient.ContainsKey(po))
                {
                    _orderToClient[po].Remove(id);
                    if (_orderToClient[po].Count == 0)
                    {
                        _orderToClient.Remove(po);
                    }
                }
                return true;
            }
            return false;
        }

        public bool RemoveParticipant(ParticipantOrder po)
        {
            if (_orderToClient.ContainsKey(po) && _orderToClient[po].Count > 0)
            {
                var ids = new List<ulong>(_orderToClient[po]);
                foreach (var id in ids)
                {
                    _clientToOrder.Remove(id);
                }
                _orderToClient.Remove(po);
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