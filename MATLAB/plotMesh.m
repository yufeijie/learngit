function plotMesh(titleName, xLabelName, yLabelName, zLabelName, x, y, z)
% plotMesh 画三维曲线图
% titleName 标题名
% xLabelName X轴名
% yLabelName Y轴名
% zLabelName Z轴名
% x y z 数据
figure(2)
mesh(x, y, z);
colormap(flipud(jet));
colorbar;
shading interp;
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end