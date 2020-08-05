function plotXYZ(titleName, xLabelName, yLabelName, zLabelName, n, len, x, y, z)
% plotXYZ 画三维曲线图
% titleName 标题名
% xLabelName X轴名
% yLabelName Y轴名
% zLabelName Z轴名
% n 数据系列数
% len 数据长度
% x y z 数据
figure(1)
for i = 1:n
    xx = x(i, 1:len(i));
    yy = y(i, 1:len(i));
    zz = z(i, 1:len(i));
    plot3(xx, yy, zz);
    hold on
end
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end