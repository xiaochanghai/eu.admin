# 服务器添加sudo权限和免密

## 登录

登陆或切换到root用户下，以 windows 11 终端为例

## 添加sudo文件的写权限

使用 `chmod u+w /etc/sudoers` 命令添加对sudo文件的写入权限

## 编辑sudoers文件

使用 `vi /etc/sudoers` 命令编辑 sudoers 文件

找到这行 `root ALL=(ALL) ALL`,在他下面添加xxx ALL=(ALL) ALL (注：这里的xxx是你的用户名)  

你可以根据实际需要在sudoers文件中按照下面四行格式中任意一条进行添加：  

``` bash
youuser ALL=(ALL) ALL  
%youuser ALL=(ALL) ALL  
youuser ALL=(ALL) NOPASSWD: ALL  
%youuser ALL=(ALL) NOPASSWD: ALL  
```

第一行：允许用户youuser执行sudo命令(需要输入密码)。  
第二行：允许用户组youuser里面的用户执行sudo命令(需要输入密码)。  
第三行：允许用户youuser执行sudo命令,并且在执行的时候不输入密码。  
第四行：允许用户组youuser里面的用户执行sudo命令,并且在执行的时候不输入密码。  

## 撤销sudoers文件写权限

使用 `chmod u-w /etc/sudoers` 命令撤销写入权限

``` bash
# 登录服务器
ssh root@192.168.xx.xx
root@192.168.xx.xx's password:
Last login: Tue Jun 14 02:48:13 2022 from 192.168.xx.1
# 添加写入权限
[root@localhost ~]# chmod u+w /etc/sudoers
# 编辑文件
[root@localhost ~]# vi /etc/sudoers
# 撤销写入权限
[root@localhost ~]# chmod u-w /etc/sudoers
```
