function plotSurf(titleName, xLabelName, yLabelName, zLabelName, x, y, z)
% plotSurf 画三维曲面图（反JET）
% titleName 标题名
% xLabelName X轴名
% yLabelName Y轴名
% zLabelName Z轴名
% x y z 数据
figure(3)
surf(x, y, z);
colormap(flipud(jet));
colorbar;
shading interp;
grid on
title(titleName)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)
end