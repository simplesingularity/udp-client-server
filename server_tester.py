import socket

HOST = '0.0.0.0' 
PORT = 44444 
BUFFER_SIZE = 1024    
s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
s.bind((HOST, PORT))
while True:
    data = s.recvfrom(BUFFER_SIZE)
    if data:
        print('Client to Server: ' , data)
        s.sendto("\x04\x00\x00\x00\x04\x00\x00\x00Pong", data[1])
s.close()