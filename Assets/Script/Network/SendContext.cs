using System.Threading;
using System;
using UnityEngine;
using System.Drawing;
using Hunt.Common;
using Unity.VisualScripting;
using System.Linq;

//쓰기 버퍼 1개
//읽기 버퍼 1개(전송하는 버퍼)
//쓰기는 읽기 버퍼까지 상호 배제
//읽기 버퍼를 가져가면, 더이상 읽기 버퍼는 상호배제 하지 않음
//  send하는 동안 lock 잡으면 안되잖음.

namespace hunt.Net
{
    class InternalSendBuffer
    {
        byte[] m_data;
        UInt32 m_size = 0;
        UInt32 m_cap = 0;
        public InternalSendBuffer()
        {
            m_data = new byte[2000];
            m_cap = 2000;
            m_size = 0;
        }

        public void Write(byte[] writeData, UInt32 len)
        {
            if (m_cap < len + m_size)//크기가 작다면, 1.5배합니다
            {
                UInt32 newLen = m_cap + m_cap / 2;
                byte[] data = new byte[newLen];
                Buffer.BlockCopy(m_data, 0, data, 0, (int)m_size);
                m_data = data;
                m_cap = newLen;
            }

            //버퍼에 복사
            Buffer.BlockCopy(writeData, 0, m_data, (int)m_size, (int)len);
            m_size += len;
        }
        public void Clear()
        {
            m_size = 0;
        }

        public byte[] GetData()
        {
            return m_data;
        }

        public UInt32 GetLength()
        {
            return m_size;
        }
    }

    class SendContext
    {
        private readonly object m_lock = new object(); // Mutex보다 lock이 더 가볍고 안전
        InternalSendBuffer[] m_sendBuffers;
        int m_writeAbleIdx = 0;
        bool m_isLittleEndian = false;

        public SendContext(bool isLittleEndian)
        {
            m_isLittleEndian = isLittleEndian;
            m_sendBuffers = new InternalSendBuffer[2];
            m_sendBuffers[0] = new InternalSendBuffer();
            m_sendBuffers[1] = new InternalSendBuffer();
        }

        public void Send(UInt32 msgId, byte[] writeData2, UInt16 len2) //writeableIdx 버퍼에 write작업
        {
            //totalSize는 TCP는 Stream형태이기때문에, 하나의 패킷의 경계를 알 수 없기때문에, 크기를 보내야함
            UInt16 totalSize = (UInt16)(sizeof(UInt32) + len2 + sizeof(UInt16));
            Debug.Assert(totalSize < UInt16.MaxValue);

            var sizeSerialize = BitConverter.GetBytes(totalSize);
            var msgIdSerialize = BitConverter.GetBytes(msgId);
            if (m_isLittleEndian)//little endianness -> big endianness(network byte order)
            {

                var arr = sizeSerialize.Reverse();
                sizeSerialize = arr.ToArray();

                var arr2 = msgIdSerialize.Reverse();
                msgIdSerialize = arr2.ToArray();
            }

            lock (m_lock)
            {
                //total Len: (sizeof(Uint16) + sizeof(msgId) + sizeof(payload))
                m_sendBuffers[m_writeAbleIdx].Write(sizeSerialize, sizeof(UInt16));
                //Msg Id
                m_sendBuffers[m_writeAbleIdx].Write(msgIdSerialize, sizeof(UInt32));
                //payload
                m_sendBuffers[m_writeAbleIdx].Write(writeData2, len2);
            }
        }
        //writeableIdx를 바꾸고, 더 이상 write할일 없는 prev에 대해서 전송 버퍼로 활용
        public InternalSendBuffer GetSendAbleData()//writeableIdx를 교체, 지금까지 쓰여지던 버퍼는 send, 안쓰던 버퍼는 write작업으로
        {
            var prev = 0;
            lock (m_lock)
            {
                prev = m_writeAbleIdx;
                m_writeAbleIdx = (m_writeAbleIdx + 1) & 1;//알아서 0, 1 변경 됨
                m_sendBuffers[m_writeAbleIdx].Clear();//쉬던 버퍼를 클리어 -> 이제 이 버퍼에 write작업이 들어감
            }
            return m_sendBuffers[prev];//지금까지 send하기 모아온 데이터를 리턴
        }
    }
}