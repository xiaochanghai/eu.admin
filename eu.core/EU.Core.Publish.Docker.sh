﻿# 停止容器
docker stop apkcontainer
# 删除容器
docker rm apkcontainer
# 删除镜像
docker rmi laozhangisphi/apkimg
# 切换目录
cd /home/Tiobon.Core
# 发布项目
./Tiobon.Core.Publish.Linux.sh
# 进入目录
cd /home/Tiobon.Core/.PublishFiles
# 编译镜像
docker build -t laozhangisphi/apkimg .
# 生成容器
docker run --name=apkcontainer -d -v /etc/localtime:/etc/localtime -it -p 9291:9291 laozhangisphi/apkimg
# 启动容器
docker start apkcontainer
