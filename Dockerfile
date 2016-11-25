FROM mono:latest
ADD ./bin/Release /root
ENTRYPOINT [ "mono",  "/root/OpenStreetMapCache.exe", "-s" ]
EXPOSE 2000
