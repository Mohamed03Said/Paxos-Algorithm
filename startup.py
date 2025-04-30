import requests
import threading
import random
import time

ports = [7166, 7167, 7168, 7169, 7170]

def initialize_node(port):
    delay = random.randint(1000, 3000)
    url = f"http://localhost:{port}/Paxos/initialize/{delay}"
    print(f"Initializing node on port {port} with delay {delay}ms")

    time.sleep(delay / 1000)

    try:
        response = requests.get(url)
    except requests.RequestException as e:
        print(f"Node {port} failed to initialize: {e}")

def main():
    
    print(f"\n\n-------  Application Started  -------\n\n")
    
    threads = []

    for port in ports:
        t = threading.Thread(target=initialize_node, args=(port,))
        threads.append(t)
        t.start()

    for t in threads:
        t.join()
        
    
    print(f"\n\n-------  Application Finished  -------\n\n")

if __name__ == "__main__":
    main()

