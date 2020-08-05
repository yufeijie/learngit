function plotPareto(headline, subTitle1, subTitle2, subTitle3, subTitle4, xLabelName, yLabelName, zLabelName, size, data, dataPareto, nMin, nMax)
% plotPareto 画Pareto图
% headline 大标题
% subTitle1 2 3 4 子标题
% xLabelName yLabelName zLabelName 坐标轴名
% size 数据总数
% data  数据
% dataPareto Pareto最优数据
global sz mkr1 mkr2 Nmin Nmax
sz = 12;
mkr1 = 'o';
mkr2 = 'o';
Nmin = double(nMin);
Nmax = double(nMax);

figure(7)

ax1 = subplot(3,2,1);
plot3InDiffType(ax1, data(1:size, 1), data(1:size, 2), data(1:size, 3), data(1:size, 4), data(1:size, 8), 0);
plot3InDiffType(ax1, dataPareto(:, 1), dataPareto(:, 2), dataPareto(:, 3), dataPareto(:, 4), dataPareto(:, 8), 1);
grid on
title(subTitle1)
xlabel(xLabelName)
ylabel(yLabelName)
zlabel(zLabelName)

m = length(dataPareto(:, 1));

ax2 = subplot(3,2,2);
[plot1, plot2] = plotInDiffType(ax2, data(1:size, 1), data(1:size, 2), data(1:size, 4), data(1:size, 8), 0);
[plot3, plot4] = plotInDiffType(ax2, dataPareto(:, 1), dataPareto(:, 2), dataPareto(:, 4), dataPareto(:, 8), 1);
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 2) < dataPareto(j, 2)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 1);
pareto(1, 2) = dataPareto(1, 2);
for i = 2 : m
    while n > 0 && dataPareto(i, 1) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 1);
    pareto(n, 2) = dataPareto(i, 2);
end
plot5 = plot(pareto(1:n, 1), pareto(1:n, 2), 'r');
grid on
title(subTitle2)
xlabel(xLabelName)
ylabel(yLabelName)

ax3 = subplot(3,2,3);
plotInDiffType(ax3, data(1:size, 2), data(1:size, 3), data(1:size, 4), data(1:size, 8), 0);
plotInDiffType(ax3, dataPareto(:, 2), dataPareto(:, 3), dataPareto(:, 4), dataPareto(:, 8), 1);
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 3) > dataPareto(j, 3)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 2);
pareto(1, 2) = dataPareto(1, 3);
for i = 2 : m
    while n > 0 && dataPareto(i, 2) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 2);
    pareto(n, 2) = dataPareto(i, 3);
end
plot(pareto(1:n, 1), pareto(1:n, 2), 'r');
grid on
title(subTitle3)
xlabel(yLabelName)
ylabel(zLabelName)

ax4 = subplot(3,2,4);
plotInDiffType(ax4, data(1:size, 1), data(1:size, 3), data(1:size, 4), data(1:size, 8), 0);
plotInDiffType(ax4, dataPareto(:, 1), dataPareto(:, 3), dataPareto(:, 4), dataPareto(:, 8), 1);
for i = 1 : m-1
    for j = i+1 : m
        if dataPareto(i, 3) > dataPareto(j, 3)
            dataPareto([i, j], :) = dataPareto([j, i], :);
        end
    end
end
n = 1;
pareto(1, 1) = dataPareto(1, 1);
pareto(1, 2) = dataPareto(1, 3);
for i = 2 : m
    while n > 0 && dataPareto(i, 1) < pareto(n, 1)
        n = n-1;
    end
    n = n+1;
    pareto(n, 1) = dataPareto(i, 1);
    pareto(n, 2) = dataPareto(i, 3);
end
plot(pareto(1:n, 1), pareto(1:n, 2), 'r');
grid on
title(subTitle4)
xlabel(xLabelName)
ylabel(zLabelName)

colorbar(ax3, 'south', 'Ticks', linspace(0, 1, 8), 'TickLabels', {'30', '40', '50', '60', '70', '80', '90', '100'});
c = colorbar(ax4, 'south', 'Ticks', []);
c.Label.String = 'Module number';
legend(ax4, [plot1, plot2, plot3 ,plot4 ,plot5], {'Feasible design based on SiC', 'Feasible design based on IGBT', '3D Pareto design', '3D Pareto design', '2D Pareto front'}, 'Location', 'east');
%legend('boxoff')

%suptitle(headline)
end



function [plot1, plot2] = plot3InDiffType(ax, x, y, z, c, type, pattern)
global sz mkr1 mkr2
n = length(x);
x1 = zeros(n, 1);
y1 = zeros(n, 1);
z1 = zeros(n, 1);
c1 = zeros(n, 1);
len1 = 0;
x2 = zeros(n, 1);
y2 = zeros(n, 1);
z2 = zeros(n, 1);
c2 = zeros(n, 1);
len2 = 0;
for i = 1 : n
    if type(i) == 1
        len1 = len1+1;
        x1(len1) = x(i);
        y1(len1) = y(i);
        z1(len1) = z(i);
        c1(len1) = c(i);
    else
        len2 = len2+1;
        x2(len2) = x(i);
        y2(len2) = y(i);
        z2(len2) = z(i);
        c2(len2) = c(i);
    end
    
end
plot1 = scatter3(ax, x1(1:len1), y1(1:len1), z1(1:len1), sz, getCmap(c1(1:len1), 1), mkr1, 'filled');
hold on
plot2 = scatter3(ax, x2(1:len2), y2(1:len2), z2(1:len2), sz, getCmap(c2(1:len2), 0), mkr2, 'filled');
hold on
if pattern == 1
    plot1.LineWidth = 0.75;
    plot1.MarkerEdgeColor = 'r';
    plot2.LineWidth = 0.75;
    plot2.MarkerEdgeColor = 'r';
end
hold on
end

function [plot1, plot2] = plotInDiffType(ax, x, y, c, type, pattern)
global sz mkr1 mkr2
n = length(x);
x1 = zeros(n, 1);
y1 = zeros(n, 1);
c1 = zeros(n, 1);
len1 = 0;
x2 = zeros(n, 1);
y2 = zeros(n, 1);
c2 = zeros(n, 1);
len2 = 0;
for i = 1 : n
    if type(i) == 1
        len1 = len1+1;
        x1(len1) = x(i);
        y1(len1) = y(i);
        c1(len1) = c(i);
    else
        len2 = len2+1;
        x2(len2) = x(i);
        y2(len2) = y(i);
        c2(len2) = c(i);
    end
    
end
plot1 = scatter(ax, x1(1:len1), y1(1:len1), sz, getCmap(c1(1:len1), 1), mkr1, 'filled');
hold on
plot2 = scatter(ax, x2(1:len2), y2(1:len2), sz, getCmap(c2(1:len2), 0), mkr2, 'filled');
hold on
if pattern == 1
    plot1.LineWidth = 0.5;
    plot1.MarkerEdgeColor = 'r';
    plot2.LineWidth = 0.5;
    plot2.MarkerEdgeColor = 'r';
end
end

function cmap = getCmap(c, type)
global Nmin Nmax
shift = 0.4;
n = length(c);
cmap = zeros(n, 3);
for i = 1 : n
    if type == 1
        normal = (c(i)-Nmin)/(Nmax-Nmin);
        delta = (1-shift)*normal;
        cmap(i, 1) = delta;
        cmap(i, 2) = delta;
        cmap(i, 3) = shift+delta;
    else
        normal = (c(i)-Nmin)/(Nmax-Nmin);
        delta = (1-shift)*normal;
        cmap(i, 1) = delta;
        cmap(i, 2) = shift+delta;
        cmap(i, 3) = delta;
    end
end
end